# Core Query Building

The `SqlBuilder` is the primary entry point for constructing parameterized, dialect-aware SQL queries using C# interpolated strings. It maintains a stateful sequence of query segments and tracks your registered entity variables.

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
    // result.Sql -> "SELECT * FROM [Products] WHERE [Id] = @p0"
    // result.Parameters -> { ["@p0"] = 42 }
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