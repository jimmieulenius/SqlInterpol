# Dynamic Queries

The `SqlBuilder` is designed for stateful, forward-only query construction. You can incrementally build complex SQL statements based on runtime conditions (like optional search filters) while maintaining complete parameter safety and type-safe column references.

## Conditional Appending

The most common pattern for dynamic queries is appending `WHERE` clauses based on user input. Because `SqlInterpol` securely extracts variables into parameters at the exact moment of evaluation, dynamically concatenating interpolated strings never exposes your application to SQL injection.

```csharp
public async Task<IEnumerable<Product>> SearchProductsAsync(
    IDbConnection connection, 
    string? keyword, 
    int? categoryId, 
    decimal? maxPrice)
{
    using var db = connection.CreateSqlBuilder();
    db.Entity<Product>(out var p);

    // 1. Define the base query
    db.AppendLine($"""
        SELECT {p} 
        FROM {p} 
        WHERE 1=1
        """);

    // 2. Conditionally append filters
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        // Variables are still parameterized securely
        var pattern = $"%{keyword}%";
        db.AppendLine($"AND {p.Name} LIKE {pattern}");
    }

    if (categoryId.HasValue)
    {
        db.AppendLine($"AND {p.CategoryId} = {categoryId.Value}");
    }

    if (maxPrice.HasValue)
    {
        db.AppendLine($"AND {p.Price} <= {maxPrice.Value}");
    }

    // 3. Finalize sorting or pagination
    db.AppendLine($"ORDER BY {p.CreatedAt} DESC");

    var query = db.Build();

    // Execute using your preferred integration (e.g., Dapper)
    return await connection.QueryAsync<Product>(query);
}
```

> ℹ️ **`Append` vs `AppendLine`**  
> Use `AppendLine` when building dynamic queries to ensure safe whitespace separation between clauses. If you use `Append`, be sure to include leading spaces (e.g., `db.Append($" AND {p.Id} = {id}")`) to prevent syntax errors like `WHERE 1=1AND...`.

## Dynamic Collections (`IN` Clauses)

When filtering by a dynamic list of IDs, you can conditionally append the `IN` clause. `SqlInterpol` automatically expands the array or list into the correct number of parameterized placeholders.

```csharp
public void ApplyCategoryFilter(SqlBuilder db, Product p, int[]? categoryIds)
{
    if (categoryIds is { Length: > 0 })
    {
        // Expands to: AND p."CategoryId" IN ($1, $2, $3)
        db.AppendLine($"AND {p.CategoryId} IN {categoryIds}");
    }
}
```

## Dynamic Column References

When column selection is driven by user input or configuration, use the `entity.Column(propertyName)` extension method. It resolves the physical column name from the entity's attribute mapping and emits a properly quoted reference — no `Sql.Raw()` required.

```csharp
// Validate against an allowlist first, then use .Column() for safe projection
string[] allowed = ["Name", "Price", "CreatedAt"];
if (!allowed.Contains(userColumn)) throw new ArgumentException("Invalid column.");

db.AppendLine($"SELECT {p.Column(userColumn)} FROM {p}");
```

The companion `entity.OrderBy(propertyName, SqlOrderDirection)` builds a type-safe `ORDER BY` fragment from a string name:

```csharp
public void ApplySorting(SqlBuilder db, Product p, string sortColumn, bool descending)
{
    string[] allowed = ["Name", "Price", "CreatedAt"];
    if (!allowed.Contains(sortColumn)) sortColumn = "CreatedAt";

    var direction = descending ? SqlOrderDirection.Descending : SqlOrderDirection.Ascending;
    db.AppendLine($"ORDER BY {p.OrderBy(sortColumn, direction)}");
}
```

For cases where you need to combine a strongly-typed column reference with a direction string (e.g., a switch-mapped property), `Sql.Raw()` is the correct tool for the keyword itself:

```csharp
object sortRef = sortColumn switch { "price" => p.Price, _ => p.CreatedAt };
db.AppendLine($"ORDER BY {sortRef} {Sql.Raw(descending ? "DESC" : "ASC")}");
```

## Composable Subqueries

For complex logic, you can capture isolated query blocks and embed them as subqueries within your main statement. The `Query` extension isolates the builder logic into a temporary scope, freezing the result into a strongly-typed `ISqlQuery<T>` that can be interpolated directly into the parent timeline.

Because the subquery natively shares the parent builder's entity registry and variable scope, you can reference outer entities directly inside the `Query` block to effortlessly construct correlated subqueries.

> ℹ️ **Schema attributes and entity mapping**  
> For information about `[SqlTable]`, `[SqlColumn]`, `[SqlIgnore]`, and `[SqlQueryAttribute]`, see [Schema & Entity Mapping](schema-mapping.md).

### Inline Subqueries

You can chain entity registration and subquery capture directly in your query setup:

```csharp
var activeStatus = 100;
var minPrice = 101;
var db = new SqlBuilder();

// Register outer entity, inner entity, and capture the correlated subquery
db.Entity<Product>(out var p)
  .Entity<Category>(out var c)
  .Query(c, out var categorySubquery, () => db.Append($$"""
      SELECT
          {{c.Name}}
      FROM {{c}}
      WHERE {{c.Id}} = {{p.CategoryId}} AND {{c.IsActive}} = {{activeStatus}}
      """));

// Interpolate the captured subquery into the main selection
var result = db.Append($$"""
    SELECT 
        {{p.Id}},
        (
            {{categorySubquery}}
        ) AS CategoryName
    FROM {{p}} AS prod
    WHERE {{p.Price}} > {{minPrice}}
    """).Build();
```

### Reusable Subquery Functions

If a specific subquery is used across multiple reports or endpoints, you can extract it into a reusable extension method. By passing the outer scope's `SqlBuilder` and entity reference (`Product p`), the helper method safely correlates the generated `ISqlQuery<Category>` to the parent query without muddying the main logic.

```csharp
public static class QueryHelpers
{
    // Helper function returning a strongly-typed subquery
    [SqlQuery]
    public static ISqlQuery<Category> BuildCategorySubquery(this SqlBuilder db, Product p, int activeStatus)
    {
        return db
            .Entity<Category>(out var c)
            .Query(c, () => db.Append($$"""
                SELECT
                    {{c.Name}}
                FROM {{c}}
                WHERE {{c.Id}} = {{p.Column(nameof(p.CategoryId))}} AND {{c.IsActive}} = {{activeStatus}}
                """));
    }
}
```

You can then interpolate the result of this function directly into any outer query:

```csharp
var db = new SqlBuilder();     
db.Entity<Product>(out var p);

var result = db.Append($$"""
    SELECT 
        {{p.Id}},
        (
            {{db.BuildCategorySubquery(p, activeStatus: 100)}}
        ) AS CategoryName
    FROM {{p}} AS prod
    WHERE {{p.Price}} > 101
    """).Build();
```