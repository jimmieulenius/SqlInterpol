# Schema & Entity Mapping

`SqlInterpol` allows you to bind C# classes and properties directly to database tables, views, and columns using lightweight schema attributes. This provides compile-time safety, automatic column escaping, and context-aware aliasing without requiring verbose fluent mapping definitions.

> ℹ️ **Attribute-Free Mapping (Clean POCOs)**  
> You do **not** need to use schema attributes if you prefer to keep your domain models strictly decoupled from your database schema. By default, `SqlInterpol` uses the exact C# class and property names.
>
> If your database tables or columns differ from your C# models, you can bridge the gap directly in your SQL text using standard `AS` aliasing:
> ```csharp
> // Clean POCO with no SqlInterpol attributes
> public class Product 
> {
>     public string Name { get; set; }
> }
> 
> db.Entity<Product>(out var p);
> 
> // Map manual database names to C# properties on the fly
> var query = db.Append($"""
>     SELECT product_name_db AS {p.Name}
>     FROM catalog_products_db AS {p}
>     """).Build();
> ```

---

## Metadata Attributes & Render Modifiers

`SqlInterpol` relies on mapping attributes to resolve physical database schema details and inline render modifiers to dictate how strongly-typed objects format themselves in specific clauses.

### Entity Attributes

Apply these directly to your C# models to govern how the builder resolves physical names.

| Attribute | Target | Description | Example |
| :--- | :--- | :--- | :--- |
| `[SqlTable("name", "schema")]` | Class | Maps the class to a specific database table. | `[SqlTable("products", Schema = "catalog")]` |
| `[SqlView("name", "schema")]` | Class | Maps the class to a specific database view. | `[SqlView("v_active_orders")]` |
| `[SqlColumn("name")]` | Property | Overrides the physical column name for a specific property. | `[SqlColumn("product_name")]` |
| `[SqlEnumFormat(SqlEnumFormat)]` | Property | Overrides the global enum formatting rule (String vs Integer) for a specific property. | `[SqlEnumFormat(SqlEnumFormat.String)]` |
| `[SqlIgnore]` | Property | Instructs the mapper and expansion macros to completely ignore the property. | `[SqlIgnore]` |

> ℹ️ **Default Conventions**  
> If no attributes are specified, `SqlInterpol` defaults to using the un-namespaced class name as the table name, and exact property names as column names.

### Inline Render Modifiers

When interpolating entities or columns, use these fluent extension methods to explicitly dictate the output format.

| Modifier | Replaces Format | Output Example | Context |
| :--- | :--- | :--- | :--- |
| **`.AsDeclaration()`** | `:decl` | `[dbo].[Users] AS [u]` | `FROM` or `JOIN` clauses. |
| **`.AsAlias()`** | `:alias` | `[u]` | Selecting an entire entity (e.g., `SELECT {u.AsAlias()}.*`). |
| **`.AsBase()`** | `:base` | `[dbo].[Users]` | DDL operations or dialect-specific scopes requiring schema-only names. |
| **`.AsColumn()`** | `:col` | `[user_id]` | Emitting the bare physical column name without table aliases. |

---

## Defining Mapped Entities

```csharp
using SqlInterpol.Schema;

namespace MyApp.Domain;

public enum OrderStatus { Pending = 0, Processing = 1, Completed = 2 }

[SqlTable("orders", Schema = "sales")]
public class Order
{
    [SqlColumn("order_id")]
    public int Id { get; set; }

    [SqlColumn("customer_id")]
    public int CustomerId { get; set; }

    [SqlColumn("total_amount")]
    public decimal TotalAmount { get; set; }

    // Overrides global options to serialize this enum as a string in SQL queries
    [SqlEnumFormat(SqlEnumFormat.String)]
    [SqlColumn("status_code")]
    public OrderStatus Status { get; set; }

    // Ignored properties are omitted from SQL generation
    [SqlIgnore]
    public bool IsSelectedInUi { get; set; }
}
```

---

## Mapping Views

To query database views, decorate your model with `[SqlView]`:

```csharp
[SqlView("v_order_summaries", Schema = "analytics")]
public class OrderSummaryView
{
    [SqlColumn("order_id")]
    public int OrderId { get; set; }

    [SqlColumn("customer_name")]
    public string CustomerName { get; set; } = string.Empty;

    [SqlColumn("item_count")]
    public int ItemCount { get; set; }
}
```

---

## Enum Serialization Overrides

The `[SqlEnumFormat]` attribute overrides global `SqlInterpolOptions.EnumFormat` settings on a per-property basis:

```csharp
public class UserAccount
{
    public int Id { get; set; }

    // Serialized as integer (0, 1, 2)
    [SqlEnumFormat(SqlEnumFormat.Integer)]
    public SecurityLevel Rank { get; set; }

    // Serialized as string ("Active", "Suspended")
    [SqlEnumFormat(SqlEnumFormat.String)]
    public AccountStatus Status { get; set; }
}
```

---

## Entity Aliasing & `AS` Syntax

Entities are bound inside query builders using the `db.Entity<T>(out var alias)` syntax. The generated object acts as a strongly-typed proxy representing both table references and member column references. 

`SqlInterpol` offers flexible support for `AS` aliasing, allowing you to use native SQL `AS` keywords, explicit override aliases, or granular interpolation formatters (`:base` and `:alias`).

### Interpolation Formatters

When interpolating an entity proxy (such as `p`), you can append format specifiers (or use the equivalent inline render modifiers like `.AsBase()`):

*   `{p}`: In a `SELECT` clause, expands to **all mapped entity columns** (`p."Id", p."Name"`). In a `FROM` clause, expands to the complete table reference and its bound alias.
*   `{p:base}`: Renders **only the base table name**, including any `[SqlTable]` schema qualification (e.g., `"sales"."products"`).
*   `{p:alias}`: Renders **only the alias identifier**.

### 1. Full Column Expansion (`SELECT {p}`) with Split `AS`

Interpolating `{p}` in the `SELECT` clause expands all entity properties qualified by the alias, while `{p:base} AS {p:alias}` explicitly splits the table definition and alias in the `FROM` clause:

```csharp
db.Entity<Product>(out var p, alias: "prod");

var query = db.Append($"""
    SELECT {p}
    FROM {p:base} AS {p:alias}
    """).Build();
```

*   **PostgreSQL:**
    ```sql
    SELECT prod."Id", prod."Name", prod."Price"
    FROM "Products" AS prod
    ```
*   **SQL Server:**
    ```sql
    SELECT [prod].[Id], [prod].[Name], [prod].[Price]
    FROM [Products] AS [prod]
    ```

### 2. Manual Alias Override (`FROM {p} AS alias`)

You can append a custom `AS` identifier directly to `{p}` in your SQL text. The compilation pipeline detects the inline `AS` and safely overrides the entity's bound alias:

```csharp
db.Entity<Product>(out var p);

var query = db.Append($"""
    SELECT {p.Id}
    FROM {p} AS prod
    """).Build();
```

*   **PostgreSQL:**
    ```sql
    SELECT prod."Id"
    FROM "Products" AS prod
    ```
*   **SQL Server:**
    ```sql
    SELECT [prod].[Id]
    FROM [Products] AS [prod]
    ```

### 3. Literal Table with Entity Alias (`FROM TABLE AS {p:alias}`)

If you are querying a raw database table or temp table while retaining type safety for column projections, use `{p:alias}` to bind the entity alias to a literal table name:

```csharp
db.Entity<OrderLine>(out var ol, alias: "line_item");

var query = db.Append($"""
    SELECT {ol.OrderId}
    FROM ORDER_LINES AS {ol:alias}
    """).Build();
```

*   **PostgreSQL:**
    ```sql
    SELECT line_item."OrderId"
    FROM ORDER_LINES AS line_item
    ```
*   **SQL Server:**
    ```sql
    SELECT [line_item].[OrderId]
    FROM ORDER_LINES AS [line_item]
    ```

### 4. Auto-Aliasing via `EntityAutoAliasing`

When `EntityAutoAliasing` is enabled in `SqlInterpolOptions`, the C# variable name used in `out var` (e.g., `prod`) is automatically captured via `[CallerArgumentExpression]` and used as the SQL alias. 

When auto-aliasing resolves the name, it automatically injects the `AS` keyword into the `FROM` clause if the underlying dialect permits it.

```csharp
db.Context.Options.EntityAutoAliasing = true;

db.Entity<Product>(out var prod);

var query = db.Append($"""
    SELECT {prod.Id}
    FROM {prod}
    """).Build();
```

*   **PostgreSQL:**
    ```sql
    SELECT prod."Id"
    FROM "Products" AS prod
    ```
*   **SQL Server:**
    ```sql
    SELECT [prod].[Id]
    FROM [Products] AS [prod]
    ```

### 5. Class & Property Name Resolution (`AS {p}` / `AS {p.X}`)

When using `[SqlTable]` or `[SqlColumn]` attributes to map a class to a differently named database object, you often need to alias it back to the original C# class or property name so materializers (like Dapper) can map the result set correctly.

If you interpolate an entity or property **after** an `AS` keyword, `SqlInterpol` intelligently context-switches and renders the **C# member name** instead of the mapped database name.

```csharp
[SqlTable("db_products")]
public class Product 
{
    [SqlColumn("product_name")]
    public string Name { get; set; }
}

db.Entity<Product>(out var p);

var query = db.Append($"""
    SELECT {p.Name} AS {p.Name} 
    FROM {p} AS {p}
    """).Build();
```

*   **SQL Server Output:**
    ```sql
    SELECT [p].[product_name] AS [Name] 
    FROM [db_products] AS [Product]
    ```

Notice how the exact same interpolation tokens (`{p.Name}` and `{p}`) output the mapped database names (`[product_name]` and `[db_products]`) before the `AS`, and the exact C# type/property names (`[Name]` and `[Product]`) after it.

---

## Sharing Mappings with EF Core

If your application uses Entity Framework Core alongside `SqlInterpol`, you can reuse your `SqlInterpol` schema attributes in `DbContext.OnModelCreating` to keep EF Core in sync without duplicate configuration:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Automatically maps [SqlTable], [SqlColumn], and [SqlEnumFormat] to EF Core metadata
    modelBuilder.MapSqlEntity<Order>();
    modelBuilder.MapSqlEntity<OrderSummaryView>();
}
```

> ⚠️ **Reflection & Native AOT**  
> Reading schema attributes via `modelBuilder.MapSqlEntity<T>()` uses runtime reflection marked with `[RequiresUnreferencedCode]`. For full Native AOT applications, use standard EF Core fluent API definitions instead.

---

## Runtime Name Overrides

You can override the physical table name and schema for a specific query call without touching the class attributes. This is useful for querying archive tables, partitioned tables, or temp tables that share the same column shape as a mapped model.

```csharp
// Queries "history"."products_archive" but column references use the Product attribute mapping
db.Entity<Product>(out var p, name: "products_archive", schema: "history");

var result = db.Append($"""
    SELECT {p.Id}, {p.Name}
    FROM {p}
    WHERE {p.Price} < {100m}
    """).Build();
```

**PostgreSQL:**
```sql
SELECT "products_archive"."Id", "products_archive"."prod_name"
FROM "history"."products_archive"
WHERE "products_archive"."Price" < $1
```

The `alias` parameter can be combined with name/schema overrides:
```csharp
db.Entity<Product>(out var p, alias: "arch", name: "products_archive", schema: "history");
```

---

## Dynamic Column References — `entity.Column()`

When column selection is data-driven (e.g., from a user report configuration), use the `Column(string propertyName)` extension method on any entity variable. It resolves the physical column name from the entity's mapping and emits a properly quoted reference.

```csharp
db.Entity<Product>(out var p);

// Safe dynamic column reference — no Sql.Raw() required
string userColumn = "Price"; // from a validated allowlist
db.Append($"SELECT {p.Column(userColumn)} FROM {p}");
```

> ⚠️ **Input Validation Required**  
> Always validate `userColumn` against a known allowlist before passing it to `Column()`. The method resolves against the entity's mapped properties; an unrecognised name throws `ArgumentException`.

The companion `OrderBy(string propertyName, SqlOrderDirection direction)` extension constructs a safe `ORDER BY` fragment from a string property name:

```csharp
db.AppendLine($"ORDER BY {p.OrderBy("Price", SqlOrderDirection.Descending)}");
// PostgreSQL: ORDER BY "Products"."Price" DESC
```

---

## `[SqlQueryAttribute]` — Subquery Helper Methods

Apply `[SqlQuery]` to a `static` method that returns `ISqlQuery<T>`. This signals the Roslyn analyzer that the method builds a correlated subquery and should be held to the same safety constraints as inline query construction.

```csharp
[SqlQuery]
public static ISqlQuery<Category> GetCategorySubquery(this SqlBuilder db, Product p, int activeStatus)
{
    return db
        .Entity<Category>(out var c)
        .Query(c, () => db.Append($"""
            SELECT {c.Name}
            FROM {c}
            WHERE {c.Id} = {p.Column(nameof(p.CategoryId))} AND {c.IsActive} = {activeStatus}
            """));
}
```

> ℹ️ **Method Requirements**  
> The method must be `static`. The first parameter must be `SqlBuilder` (the extension receiver). See [Dynamic Queries](dynamic-queries.md) for complete usage examples.