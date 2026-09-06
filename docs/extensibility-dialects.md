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
public class LegacyDatabaseDialect : SqlServerDialect // Inherit to get basic quoting/formatting
{
    public override SqlDialectKind Kind => SqlDialectKind.Custom;
    public override string ParameterPrefix => ":"; // Legacy DB uses colons
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
        // Set the global dialect
        options.Dialect = new LegacyDatabaseDialect();
        
        // Register the extension into the engine
        options.AddExtension(new LegacyDatabaseExtension());
    });
}
```