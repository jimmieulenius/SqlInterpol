# Roslyn Analyzers

`SqlInterpol` ships a Roslyn Analyzer package (`SqlInterpol.Analyzers`) that provides real-time diagnostics in Visual Studio, Rider, and the `dotnet build` pipeline. The analyzers are included automatically when you install the core `SqlInterpol` package — no extra setup is needed.

Severities can be adjusted per-project via `.editorconfig` using standard Roslyn diagnostic suppression:
```ini
[*.cs]
dotnet_diagnostic.SQLIA01.severity = error
dotnet_diagnostic.SQLIA07.severity = none
```

---

## Diagnostics Reference

### SQLIA01 — Require Interpolated String
**Severity:** Warning  
**Category:** Security

Passing a raw `string` variable, string literal, or string-concatenation expression directly to `Append()` or `AppendLine()` bypasses parameterization and may expose your application to SQL injection.

**Incorrect**
```csharp
string filter = "IsActive = 1";
db.Append(filter);
```

**Correct**
```csharp
db.Append($"WHERE {p.IsActive} = {true}");
```

When a raw SQL keyword or identifier is genuinely needed, use `Sql.Raw()` with a validated allowlist:

```csharp
string[] allowed = ["ASC", "DESC"];
if (!allowed.Contains(direction)) throw new ArgumentException();
db.Append($"ORDER BY {p.Name} {Sql.Raw(direction)}");
```

---

### SQLIA02 — Build Assignments Mismatch
**Severity:** Error  
**Category:** Correctness

Raised when a DTO passed to `Sql.BuildAssignments()` contains a property that does not exist on the target entity. This prevents silent column-not-found errors at runtime.

**Incorrect**
```csharp
// ProductDto has "ShippingWeight" but Product does not
Sql.BuildAssignments(entity, new ProductDto { ShippingWeight = 1.5 }, context);
```

**Correct**
```csharp
Sql.BuildAssignments(entity, new ProductDto { Name = "Widget", Price = 9.99m }, context);
```

---

### SQLIA03 — String Column Mismatch
**Severity:** Warning  
**Category:** Correctness

Raised when a string column selector (e.g., `p.Column("PropName")`) references a property name that does not exist on the entity type.

**Incorrect**
```csharp
// "ShippingWeight" is not a property on Product
db.Append($"SELECT {p.Column("ShippingWeight")} FROM {p}");
```

**Correct**
```csharp
db.Append($"SELECT {p.Column("Name")} FROM {p}");
```

---

### SQLIA04 — Unintentional System Method Call
**Severity:** Warning  
**Category:** Correctness

Raised when a .NET system method (`ToString()`, `GetHashCode()`, etc.) is called on an entity proxy inside an interpolated string. This usually indicates a mistyped column access.

**Incorrect**
```csharp
// p.ToString() returns a C# string, not a column reference
db.Append($"SELECT {p.ToString()}");
```

**Correct**
```csharp
// Access the property directly
db.Append($"SELECT {p.Name}");
```

---

### SQLIA05 — Unsupported Dialect Feature
**Severity:** Error  
**Category:** Usage

Raised when a SQL feature used in an interpolated string is not supported by the configured dialect. This prevents what would otherwise be a runtime `SqlDialectException` in production.

**Incorrect**
```csharp
// Builder configured for MySQL
using var db = SqlBuilder.MySql();
db.Entity<Product>(out var p);

// RETURNING is not supported by MySQL
db.Append($"INSERT INTO {p} ({p.Name}) VALUES ({name}) RETURNING {p.Id}");
```

**Correct**
```csharp
// Builder configured for PostgreSQL
using var db = SqlBuilder.PostgreSql();
db.Entity<Product>(out var p);

db.Append($"INSERT INTO {p} ({p.Name}) VALUES ({name}) RETURNING {p.Id}");
```

Supported feature detection is based on the `SqlBuilder` factory method used at the call site (`SqlBuilder.MySql()`, `SqlBuilder.PostgreSql()`, etc.).

---

### SQLIA06 — Missing `[SqlDialectAttribute]`
**Severity:** Error  
**Category:** AOT

Raised when a class implementing `ISqlDialect` does not have a `[SqlDialect]` attribute. The AOT source generator requires this attribute to extract identifier-quoting rules at build time without executing runtime code.

**Incorrect**
```csharp
public class MyDialect : SqlDialectBase 
{ 
    // Missing [SqlDialect] attribute
}
```

**Correct**
```csharp
[SqlDialect(OpenQuote = "\"", CloseQuote = "\"")]
public class MyDialect : SqlDialectBase 
{ 
    // Identifier quoting characters declared for AOT generator
}
```

---

### SQLIA07 — Template Initialized on Execution Path
**Severity:** Warning  
**Category:** Performance

Raised when `db.Template(...)` is invoked inside an instance method or request handler. Templates initialized on every call allocate memory and negate the purpose of pre-compilation.

**Incorrect**
```csharp
public IEnumerable<Order> GetOrders(int customerId)
{
    // Template re-compiled on every invocation
    db.Template(out var template, $"SELECT {o.Id} FROM {o} WHERE {o.CustomerId} = {Sql.Arg("Id")}");
    return db.Append(template, new { Id = customerId }).Build();
}
```

**Correct**
```csharp
public static class Queries
{
    // Pre-compiled once as a static template
    public static readonly ISqlTemplate GetOrders = CompileGetOrders();

    private static ISqlTemplate CompileGetOrders()
    {
        using var db = SqlBuilder.PostgreSql();
        db.Entity<Order>(out var o);
        db.Template(out var t, $"SELECT {o.Id} FROM {o} WHERE {o.CustomerId} = {Sql.Arg("Id")}");
        return t;
    }
}
```

---

## Severity Summary

| Diagnostic ID | Description | Default Severity |
| :--- | :--- | :--- |
| `SQLIA01` | Raw string passed to `Append` / `AppendLine` | Warning |
| `SQLIA02` | `BuildAssignments` DTO/entity property mismatch | Error |
| `SQLIA03` | `entity.Column()` string references unknown property | Warning |
| `SQLIA04` | System method call inside interpolated query | Warning |
| `SQLIA05` | SQL feature not supported by the configured dialect | Error |
| `SQLIA06` | Custom dialect missing `[SqlDialectAttribute]` | Error |
| `SQLIA07` | `Template()` called on an instance execution path | Warning |