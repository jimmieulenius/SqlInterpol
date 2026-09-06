# Entity Framework Core Integration

The `SqlInterpol.EntityFrameworkCore` package bridges type-safe AST generation with EF Core's execution pipeline. It automatically handles dialect resolution via provider names and materializes native `DbParameter` instances for your active database connection.

## Installation

```bash
dotnet add package SqlInterpol.EntityFrameworkCore
```

## Query Execution

The package extends `DbContext` with zero-friction execution methods. It automatically routes your interpolated arguments into provider-specific parameters (e.g., `NpgsqlParameter` or `SqliteParameter`) without hardcoding any database dependencies.

```csharp
using SqlInterpol.EFCore;

public async Task<List<Product>> GetActiveProductsAsync(AppDbContext context, int categoryId)
{
    // Automatically detects the dialect from the EF Core provider
    using var db = context.CreateSqlBuilder();
    db.Entity<Product>(out var p);

    var query = db.Append($"""
        SELECT * FROM {p}
        WHERE {p.CategoryId} = {categoryId} AND {p.IsActive} = {true}
        ORDER BY {p.Name}
        """).Build();

    // Composable IQueryable execution
    return await context.FromSql<Product>(query).ToListAsync();
}
```

**Supported Direct Extensions:**
*   `FromSql<TEntity>(SqlQueryResult)`: Returns a composable `IQueryable<TEntity>`.
*   `ExecuteSql(SqlQueryResult)`: Executes a non-query and returns rows affected.
*   `ExecuteSqlAsync(SqlQueryResult)`: Asynchronously executes a non-query.

> ℹ️ **Native parameters**  
> If you need to manually attach parameters to an existing `DbCommand`, you can use the `query.ToDbParameters(context)` extension method to extract the provider-native parameter array.

## Schema Mapping Integration

If you use `SqlInterpol`'s schema attributes (`[SqlTable]`, `[SqlView]`, `[SqlColumn]`, `[SqlEnumFormat]`), you can share that exact mapping with EF Core to avoid repeating your configuration.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Automatically applies [SqlTable], [SqlColumn], and enum conversions to EF Core
    modelBuilder.MapSqlEntity<Product>();
    modelBuilder.MapSqlEntity<Order>();
}
```

> ⚠️ **Native AOT Limitation**  
> The `MapSqlEntity<T>` extension relies on runtime reflection to read your entity metadata. It is marked with `[RequiresUnreferencedCode]` and is not compatible with full IL trimming or Native AOT environments.

## Dialect Auto-Detection

When you call `context.CreateSqlBuilder()`, the engine first inspects `context.Database.ProviderName`. This relies on stable, versioned package identifiers (e.g., `Npgsql.EntityFrameworkCore.PostgreSQL` or `Pomelo.EntityFrameworkCore.MySql`). This makes dialect resolution immune to connection-wrapping middleware.

If you use a non-standard EF Core provider, you can register a custom provider resolver globally:

```csharp
SqlInterpolEFCoreExtensions.ProviderResolvers.Add(providerName => 
    providerName == "MyCustom.Provider" ? new CustomDialect() : null);
```