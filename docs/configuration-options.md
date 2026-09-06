# Configuration & Options

`SqlInterpol` configuration is managed via `SqlInterpolOptions`, a C# `record` controlling parameter naming, collection rendering, enum formatting, cross-dialect transpilation, and compiler pipeline extensibility.

Option resolution follows a tiered fallback pattern:
1. **Dialect Defaults:** Active database dialects (`ISqlDialect.GetDefaultOptions()`) provide baseline defaults (e.g., parameter prefixes and maximum parameter limits).
2. **Global Overrides:** Set application-wide default factories via `SqlInterpolOptions.DefaultFactory` or DI.
3. **Per-Instance Overrides:** Pass custom options or use C# `with` expressions when creating a `SqlBuilder`.

---

## Configuring Defaults

### Global Default Factory

Configure `SqlInterpolOptions.DefaultFactory` once during application startup:

```csharp
using SqlInterpol.Configuration;

SqlInterpolOptions.DefaultFactory = () => new SqlInterpolOptions
{
    CrossDialectSqlTranspilation = true,
    EnumFormat = SqlEnumFormat.String,
    CollectionLayout = SqlCollectionLayout.Vertical
};
```

### Dependency Injection

Register the engine in your DI container to configure options globally across services:

```csharp
builder.Services.AddSqlInterpol(options =>
{
    options.EntityAutoAliasing = true;
    options.EnumFormat = SqlEnumFormat.String;
});
```

### Per-Instance Overrides

Because `SqlInterpolOptions` is a `record`, you can create modified copies using `with` expressions or pass new instances directly to `CreateSqlBuilder()`:

```csharp
var customOptions = baseOptions with 
{ 
    CollectionLayout = SqlCollectionLayout.Horizontal,
    QueryParametersMaxCount = 500
};

using var db = connection.CreateSqlBuilder(customOptions);
```

---

## Rendering & Formatting Options

### `CollectionLayout`
* **Type:** `SqlCollectionLayout?`
* **Default Fallback:** `SqlCollectionLayout.Horizontal`

Controls how collections and arrays in interpolated parameters are formatted within SQL clauses (e.g., `IN (...)`).

| Value | Description | Rendered SQL Example |
| :--- | :--- | :--- |
| `SqlCollectionLayout.Horizontal` | Renders elements on a single inline line. | `WHERE id IN ($1, $2, $3)` |
| `SqlCollectionLayout.Vertical` | Formats elements vertically with indentations for readable logs. | `WHERE id IN (`<br>&nbsp;&nbsp;&nbsp;&nbsp;`$1,`<br>&nbsp;&nbsp;&nbsp;&nbsp;`$2`<br>`)` |

---

### `CollectionSeparator`
* **Type:** `string?`
* **Default Fallback:** `", "`

Specifies the string inserted between elements when expanding collections.

---

### `IndentSize`
* **Type:** `int?`
* **Default Fallback:** `4`

Sets the number of spaces used for indentation when `CollectionLayout` is set to `SqlCollectionLayout.Vertical`.

---

### `EnumFormat`
* **Type:** `SqlEnumFormat?`
* **Default Fallback:** `SqlEnumFormat.Integer`

Controls how C# `enum` values are rendered and parameterized.

| Value | Description | Parameter Output Example |
| :--- | :--- | :--- |
| `SqlEnumFormat.Integer` | Serializes enums as their underlying numeric values. | `@p0 = 1` |
| `SqlEnumFormat.String` | Serializes enums as their string names (`ToString()`). | `@p0 = 'Active'` |

> ⚠️ **Attribute Override**  
> Entity properties annotated with `[SqlEnumFormat]` will always override this setting for that specific column.

---

### `ParameterIndexStart`
* **Type:** `int?`
* **Default Fallback:** Dialect default (typically `0`)

Specifies the starting index for auto-generated parameter names (e.g., `0` produces `@p0`, `@p1`; `1` produces `@p1`, `@p2`).

---

### `ParameterPrefixOverride`
* **Type:** `string?`
* **Default Fallback:** Dialect default (e.g., `"@"` for SQL Server, `":"` for Oracle)

Overrides the active dialect's default parameter prefix character.

---

## Behavior & Transpilation Options

### `CrossDialectSqlTranspilation`
* **Type:** `bool?`
* **Default Fallback:** `true`

When enabled, the compilation pipeline automatically transpiles standard meta-SQL features (such as `LIMIT` / `OFFSET` or cross-dialect functions) into syntax compatible with the target database dialect.

```csharp
options.CrossDialectSqlTranspilation = true;
```

---

### `EntityAutoAliasing`
* **Type:** `bool?`
* **Default Fallback:** `false`

When enabled, the engine uses `[CallerArgumentExpression]` metadata to automatically convert variable names used in `db.Entity<T>(out var x)` into SQL aliases for generated entities.

```csharp
options.EntityAutoAliasing = true;

db.Entity<Product>(out var p);
// Output SQL table binding: Product AS p
```

---

### `QueryParametersMaxCount`
* **Type:** `int?`
* **Default Fallback:** Dialect native maximum (or `999` baseline)

Sets a global ceiling on the maximum number of parameters permitted in a single compiled query. Exceeding this limit causes query construction to throw an exception, preventing database driver failure.

---

## Compiler Pipeline & Extensibility

`SqlInterpolOptions` provides direct access to the query compilation pipeline for custom syntax extensions.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Rewriters` | `SqlSegmentRewriterCollection` | List of structural rewriter passes (`SqlCoreSyntaxRewriter`, `SqlSelectIntoRewriter`, `SqlMultiTableDmlRewriter`). |
| `PreprocessorRules` | `List<ISqlPreprocessorRule>` | Pipeline of custom lexical rules executed prior to core preprocessing. |
| `KeywordTags` | `Dictionary<string, string[]>` | Registry mapping custom SQL keywords to lexical tags for syntax identification. |
| `Preprocessor` | `ISqlSegmentPreprocessor?` | Overrides the default preprocessor instance. |
| `Renderer` | `ISqlSegmentRenderer?` | Overrides the default segment rendering engine. |

---

## Telemetry & Monitoring

### `OnQueryBuilt`
* **Type:** `Action<SqlQueryTelemetry>?`
* **Default:** `null`

Provides an optional callback invoked whenever a query is compiled. When set to `null`, allocation and timing overhead for telemetry collection are completely bypassed.

The `SqlQueryTelemetry` struct exposes:

| Property | Type | Description |
| :--- | :--- | :--- |
| `CallerMemberName` | `string` | The name of the method that called `Build()`. |
| `FilePath` | `string` | The source file path of the `Build()` call site. |
| `LineNumber` | `int` | The line number of the `Build()` call site. |
| `WasAotIntercepted` | `bool` | Whether the interpolated string was routed through the AOT source generator. |
| `ParameterCount` | `int` | Number of parameters extracted during this build. |
| `BuildDuration` | `TimeSpan` | Total wall-clock time for the build pass. |

```csharp
options.OnQueryBuilt = telemetry =>
{
    logger.LogDebug(
        "[{Member} L{Line}] Built {Count} params in {Ms}ms (AOT={Aot})",
        telemetry.CallerMemberName,
        telemetry.LineNumber,
        telemetry.ParameterCount,
        telemetry.BuildDuration.TotalMilliseconds,
        telemetry.WasAotIntercepted);
};
```

---

## `SqlInterpolOptionsValue` — Resolved Option Access

The `options.Value` property returns a `SqlInterpolOptionsValue` readonly struct that provides non-nullable access to all resolved option values (i.e., after dialect defaults have been applied). Dialect authors and custom renderer implementations should read options through `context.Options.Value` rather than the nullable `context.Options` properties directly.

```csharp
// Inside a custom ISqlSegmentRenderer.Render() implementation:
var layout   = context.Options.Value.CollectionLayout;   // SqlCollectionLayout (never null)
var indent   = context.Options.Value.IndentSize;          // int
var enumFmt  = context.Options.Value.EnumFormat;          // SqlEnumFormat
var maxCount = context.Options.Value.QueryParametersMaxCount; // int
```