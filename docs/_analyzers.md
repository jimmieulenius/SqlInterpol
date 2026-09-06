# Compile-Time Roslyn Analyzers

SqlInterpol ships a Roslyn Analyzer package that provides real-time diagnostics in Visual Studio, Rider, and the `dotnet build` pipeline. It acts as a pair-programmer, catching mistakes before they reach the database.

The analyzers are included automatically when you install the main `SqlInterpol` package — no extra setup needed.

## Diagnostics

### SQL Injection Prevention

Passing a raw `string` variable directly into `Append()` is a compile-time error:

```csharp
string filter = "IsActive = 1";
db.Append($"SELECT * FROM {p} WHERE {filter}"); // ❌ SI001: raw string in interpolation
```

Use `Sql.Raw()` with an allowlist when a dynamic identifier is genuinely needed:

```csharp
string[] allowed = ["ASC", "DESC"];
if (!allowed.Contains(direction)) throw new ArgumentException();
db.Append($"ORDER BY {p[x => x.Name]} {Sql.Raw(direction)}"); // ✅
```

### Unsupported Dialect Feature

Using a SQL feature that your configured dialect does not support is caught at compile time rather than at runtime:

```csharp
// Configured dialect: MySQL
db.Append($"... RETURNING {p[x => x.Id]}"); // ❌ SI002: RETURNING not supported on MySQL
```

This prevents what would otherwise be a runtime `SqlDialectException` in production.

### Invalid Column Selector

Using an unsupported expression inside a property selector is flagged immediately:

```csharp
db.Append($"WHERE {p[x => x.Price * 2]} > {100m}"); // ❌ SI003: complex expression in selector
```

### Unintentional System Method Calls

Accidentally calling a .NET method instead of a column reference is caught:

```csharp
db.Append($"SELECT {p.ToString()}"); // ❌ SI004: system method call — did you mean p[x => x.SomeProperty]?
```

### SQL Keyword Typos

Common SQL keyword misspellings (e.g. `SLECT`, `WEHRE`) are flagged as warnings.

## Severity Levels

| Analyzer | ID | Default severity |
|---|---|---|
| Raw string injection | SI001 | Error |
| Unsupported dialect feature | SI002 | Error |
| Invalid column selector | SI003 | Warning |
| Unintentional system method | SI004 | Warning |
| SQL keyword typo | SI005 | Warning |

Severities can be adjusted per-project via `.editorconfig` using standard Roslyn diagnostic suppression.
