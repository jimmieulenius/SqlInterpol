# Extensibility & Dialects

`SqlInterpol` achieves database-agnostic query generation by decoupling structural intent from string representation. The library relies on a tokenized pipeline to process and transpile queries dynamically.

## Why Create a Custom Dialect?

While `SqlInterpol` includes built-in support for major databases, you might find yourself working with a niche database engine, a legacy system with non-standard syntax, or a specific driver that requires unique behaviors. When simply changing identifier quotes or parameter prefixes isn't enough, you can build a custom `ISqlDialect` and bundle it with an `ISqlExtension` to deeply alter query structures using preprocessor rules and segment rewriters.

## Packaging & Registration (`ISqlExtension`)

To cleanly group your custom preprocessor rules, rewriters, and renderers, implement the **`ISqlExtension`** interface. This acts as a plugin that registers components directly into `SqlInterpolOptions`.

Here is how you bundle a custom `ISqlDialect` with an `ISqlExtension` to apply your pipeline modifications:

```csharp
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;

// 1. Define the Custom Dialect
// Inherit SqlDialectBase for ANSI-compatible defaults, or a built-in dialect for
// closer behaviour. [SqlDialectAttribute] is required by the AOT source generator to
// extract quoting rules at build time without executing runtime code.
[SqlDialect(OpenQuote = "\"", CloseQuote = "\"")]
public class LegacyDatabaseDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.Custom;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => ":"; // Legacy DB uses colons

    // Declare which optional SQL features this dialect supports
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } =
        new HashSet<SqlFeature> { SqlFeature.Returning };
}

// 2. Define the Extension that registers the pipeline components
public class LegacyDatabaseExtension : ISqlExtension
{
    public void Register(SqlInterpolOptions options)
    {
        // Inject our custom macro rule into the Preprocessor
        options.PreprocessorRules.Add(new LegacyTimeMacroRule());
        
        // Inject the NoLock mutator into the Rewriter pipeline
        options.Rewriters.Add(new LegacyNoLockRewriter());
    }
}
```

### `SqlFeature` — Declaring Dialect Capabilities

The `SqlFeature` enum defines all optional SQL features the engine knows about. Set `SupportedFeatures` on your dialect to control which features are available and which throw a `SqlDialectException` (or raise a `SQLIA05` analyzer error at build time).

| Value | Description |
| :--- | :--- |
| `ForUpdate` | `FOR UPDATE` row-level exclusive lock |
| `ForShare` | `FOR SHARE` row-level shared lock |
| `Returning` | `RETURNING` / `OUTPUT` clause on DML |
| `OnConflict` | `ON CONFLICT DO UPDATE` upsert syntax |
| `SelectInto` | `SELECT ... INTO #table` table creation |
| `MultiTableDelete` | `DELETE ... USING` / `DELETE ... FROM` joins |
| `MultiTableUpdate` | `UPDATE ... FROM` / `UPDATE ... JOIN` |
| `DeleteAs` | `DELETE alias FROM target AS alias ...` |
| `UpdateAs` | `UPDATE target AS alias SET ...` |
| `UpdatableInlineViews` | `UPDATE (SELECT ...) AS alias SET ...` natively |
| `CreateTableAsSelect` | `CREATE TABLE ... AS SELECT ...` fallback |

## How to Use Your Custom Dialect

You can instantiate a builder manually with your dialect, but it is usually best to apply these extensions globally using dependency injection so that every `SqlBuilder` inherits the legacy database's behaviors automatically:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqlInterpol;

public void ConfigureServices(IServiceCollection services)
{
    // Registers options, preprocessor, and renderer as singletons
    services.AddSqlInterpol(options => 
    {
        // Register the extension into the engine's default pipeline
        options.AddExtension(new LegacyDatabaseExtension());
    });
}
```

When creating a builder against the custom dialect, pass it explicitly:

```csharp
// Anywhere a builder is needed:
using var db = new SqlBuilder(new LegacyDatabaseDialect(), options);

// Or via the generic factory shorthand:
using var db = SqlBuilder.Dialect<LegacyDatabaseDialect>();
```

## Global Auto-Registration (`SqlExtensionRegistry`)

For library authors distributing an extension as a NuGet package, the preferred registration mechanism is `SqlExtensionRegistry.Register()` combined with a `[ModuleInitializer]`. This fires automatically when your assembly is loaded and applies your extension to every `new SqlInterpolOptions()` created in the host application — with zero configuration required from the consumer.

```csharp
using System.Runtime.CompilerServices;
using SqlInterpol.Configuration;

internal static class LegacyDatabaseExtensionInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // AOT-safe: registered before any SqlBuilder is created
        SqlExtensionRegistry.Register(new LegacyDatabaseExtension());
    }
}
```

In JIT environments, `SqlExtensionRegistry` also performs reflection-based auto-discovery: on first access it scans loaded assemblies whose names start with `SqlInterpol` for any concrete `ISqlExtension` implementations and registers them automatically, so no `[ModuleInitializer]` is strictly required for non-AOT targets.