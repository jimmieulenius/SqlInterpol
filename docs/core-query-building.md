# Core Query Building

The `SqlBuilder` is the primary entry point for constructing parameterized, dialect-aware SQL queries using C# interpolated strings. It maintains a stateful sequence of query segments and tracks your registered entity variables.

---

## Creating a Builder

Use the static factory methods to create a builder for a specific dialect. These are the idiomatic way to instantiate a builder outside of an ADO.NET/EF Core connection context.

```csharp
// Named factories — each caches a single shared dialect instance
var db = SqlBuilder.PostgreSql();
var db = SqlBuilder.SqlServer();
var db = SqlBuilder.MySql();
var db = SqlBuilder.SqLite();
var db = SqlBuilder.Oracle();
var db = SqlBuilder.Firebird();

// Generic factory for custom or third-party dialects
var db = SqlBuilder.Dialect<MyCustomDialect>();

// Constructor form — accepted by all integration packages
var db = new SqlBuilder(new PostgreSqlDialect(), options);
```

> ℹ️ **With options**
> All factory methods accept an optional `SqlInterpolOptions` parameter:
> ```csharp
> var db = SqlBuilder.PostgreSql(new SqlInterpolOptions { EnumFormat = SqlEnumFormat.String });
> ```

---

## Builder Mechanics

*   **`Append(...)` / `AppendLine(...)`**: Captures interpolated SQL strings, automatically parameterizing variables and preserving structural elements.
    ```csharp
    db.AppendLine($"SELECT * FROM {p} WHERE {p.Id} = {42}");
    ```
*   **`Entity<T>(out var dummy, ...)`**: Registers a model type into the local scope for query interpolation, automatically generating a query alias.
    ```csharp
    db.Entity<Product>(out var p, alias: "prod");
    ```
*   **`Query(...)`**: Captures SQL logic into an isolated, buildable query scope without affecting the segments of the outer builder.
    ```csharp
    var subquery = db.Query(() => db.Append($"SELECT {c.Id} FROM {c}"));
    db.Append($"WHERE {p.CategoryId} IN {subquery}");
    ```
*   **`Fragment(...)`**: Parses an inline string or delegate into a frozen, lightweight intermediate fragment without modifying the master statement stream, useful for dynamic loops.
    ```csharp
    var inClause = db.Fragment(b => b.Append($"{1}, {2}, {3}"));
    ```
*   **`Template(...)`**: Compiles an interpolated SQL string into a high-performance, reusable template that bypasses stream processing during execution.
    ```csharp
    var template = db.Template($"SELECT * FROM {p} WHERE {p.Id} = {Sql.Arg("Id")}");
    ```
*   **`Build(...)`**: Compiles all accumulated segments into a `SqlQueryResult` containing the final dialect-rendered SQL string and a dictionary of extracted parameters.
    ```csharp
    var result = db.Build();
    // result.Sql -> "SELECT * FROM \"Products\" WHERE \"Id\" = $1"
    // result.Parameters -> { ["$1"] = 42 }
    ```
*   **`Clear()`**: Wipes all accumulated segments and resets the internal parameter state so the builder can be reused for a new query.
    ```csharp
    db.Clear();
    ```

---

## The `Sql` Helper Class

The static `Sql` class provides explicit rendering instructions and macros for edge cases where standard interpolation is insufficient.

### `Sql.Raw(string?)`
Emits a string verbatim into the SQL output, bypassing parameterization and identifier quoting. 
> ⚠️ **Warning**  
> Use this exclusively for trusted, developer-controlled strings (e.g., hard-coded SQL expressions). Never pass user input to this method to avoid SQL injection vulnerabilities.
> ```csharp
> // Appends the raw string 'Price DESC' without parameterizing it
> db.Append($"ORDER BY {Sql.Raw("Price DESC")}");
> ```

### `Sql.Quote(string)`
Wraps the provided raw identifier in the active dialect's specific quote characters at render time (e.g., `[` and `]` for SQL Server, `"` for PostgreSQL).
```csharp
string dynamicColumn = "First Name";
// SQL Server: SELECT [First Name] FROM [Products]
db.Append($"SELECT {Sql.Quote(dynamicColumn)} FROM {p}");
```

### `Sql.Arg(string)` & `Sql.Arg<T>(Expression)`
Emits a placeholder for a runtime parameter in a pre-compiled SQL template. Evaluation is deferred until the template is executed.
```csharp
// Compiles to: ... WHERE [Id] = @p0
var template = db.Template($"SELECT * FROM {p} WHERE {p.Id} = {Sql.Arg("TenantId")}");

// Injects the TenantId dynamically at runtime
var result = executeBuilder.Append(template, new { TenantId = 42 }).Build();
```

### `Sql.Expand<TDto>()`
Instructs the processing pipeline to expand a Data Transfer Object into structural sequence fragments (like `SET` assignments or `INSERT` values).
```csharp
// Automatically expands DTO properties into parameterized INSERT values
db.Append($"INSERT INTO {p} {Sql.Expand<ProductDto>()}");
```

### `Sql.OpenQuote` / `Sql.CloseQuote`
Deferred fragments that emit the active dialect's opening and closing identifier-quote characters at render time.
```csharp
// Emits `"` on PostgreSQL and `[` / `]` on SQL Server
db.Append($"SELECT {Sql.OpenQuote}special column{Sql.CloseQuote} FROM {p}");
```

### `Sql.BuildAssignments(entity, dto, context)`
Builds a list of `ISqlAssignmentFragment` instances by matching DTO properties to mapped entity columns. Used when constructing custom DML helpers that need fine-grained control over the assignment list.
```csharp
var assignments = Sql.BuildAssignments(entity, new { Name = "Widget", Price = 9.99m }, db.Context);
```

---

## `SqlKeyword` — Safe Keyword Injection

The `SqlKeyword` class exposes every SQL keyword as a typed singleton that can be interpolated directly into a query. Because the preprocessor recognises keyword segments, they are tagged and never parameterized.

```csharp
// Compose clauses dynamically without raw string concatenation
db.Append($"{SqlKeyword.Select} {p.Id}, {p.Name}");
db.Append($"{SqlKeyword.From} {p}");
db.Append($"{SqlKeyword.Where} {p.IsActive} = {true}");
db.Append($"{SqlKeyword.OrderBy} {p.CreatedAt} {SqlKeyword.Desc}");
```

Common members: `Select`, `SelectDistinct`, `From`, `Where`, `GroupBy`, `Having`, `OrderBy`, `InnerJoin`, `LeftJoin`, `RightJoin`, `FullOuterJoin`, `CrossJoin`, `Insert`, `Update`, `Delete`, `Set`, `Values`, `With`, `Returning`, `OnConflict`, `DoUpdateSet`, `ForUpdate`, `ForShare`, `Limit`, `Offset`, `Union`, `UnionAll`, `Intersect`, `Except`, `Asc`, `Desc`, `And`, `Or`, `Not`, `In`, `Exists`, `Is`, `Null`, `As`, `On`, `Create`, `Drop`, `Truncate`, `Alter`.

---

## `SqlSetOperator` — Programmatic Set Operations

The `SqlSetOperator` enum lets you compose set operations programmatically when you don't want to hardcode the keyword as a literal.

```csharp
// Union / UnionAll / Intersect / Except
var op = userRequest.All ? SqlSetOperator.UnionAll : SqlSetOperator.Union;

db.Append($"""
    SELECT {p.Id} FROM {p}
    {op}
    SELECT {p.Id} FROM {p} WHERE {p.CategoryId} = {catId}
    """);
```

---

## Window Functions

Window functions are fully supported using standard SQL syntax. Columns referenced inside the `OVER` clause are interpolated normally and are dialect-quoted at render time.

```csharp
using var db = SqlBuilder.PostgreSql();
db.Entity<Product>(out var p);

var result = db.Append($"""
    SELECT
        {p.Name},
        SUM({p.Price}) OVER (
            PARTITION BY {p.CategoryId}
            ORDER BY {p.Id} DESC
        ) AS CategoryTotal
    FROM {p}
    """).Build();
```

**PostgreSQL:**
```sql
SELECT
    "Products"."Name",
    SUM("Products"."Price") OVER (
        PARTITION BY "Products"."CategoryId"
        ORDER BY "Products"."Id" DESC
    ) AS CategoryTotal
FROM "Products"
```

---

## Common Table Expressions (CTEs)

Use the standard `WITH` keyword. CTE aliases are treated as literal SQL text and are not quoted by the entity system.

```csharp
using var db = SqlBuilder.PostgreSql();
db.Entity<Order>(out var o);
db.Entity<OrderLine>(out var ol);

var result = db.Append($"""
    WITH recent AS (
        SELECT {o.Id}, {o.CustomerId}
        FROM {o}
        WHERE {o.CreatedAt} >= {DateTime.UtcNow.AddDays(-30)}
    )
    SELECT recent."Id", {ol.ProductId}
    FROM recent
    INNER JOIN {ol} ON recent."Id" = {ol.OrderId}
    """).Build();
```

**PostgreSQL:**
```sql
WITH recent AS (
    SELECT "Orders"."Id", "Orders"."CustomerId"
    FROM "Orders"
    WHERE "Orders"."CreatedAt" >= $1
)
SELECT recent."Id", "OrderLines"."ProductId"
FROM recent
INNER JOIN "OrderLines" ON recent."Id" = "OrderLines"."OrderId"
```