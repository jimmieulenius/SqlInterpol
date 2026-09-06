# Getting Started

`SqlInterpol` translates type-safe C# interpolated strings into dialect-specific SQL and heavily optimized native database parameters.

## 1. Installation

Install the core package and the specific integration package for your preferred data access strategy via the .NET CLI:

*   **Core Engine:** `dotnet add package SqlInterpol`
*   **Dapper Integration:** `dotnet add package SqlInterpol.Dapper`
*   **EF Core Integration:** `dotnet add package SqlInterpol.EntityFrameworkCore`

## 2. Global Configuration (Optional)

If you need to configure global options (like vertical collection layouts or default enum formatting), register the engine in your dependency injection container at startup.

```csharp
services.AddSqlInterpol(options => 
{
    options.CollectionLayout = SqlCollectionLayout.Vertical;
    options.EnumFormat = SqlEnumFormat.String;
});
```

## 3. Quick Start Examples

`SqlInterpol` integrates seamlessly with your existing data access layer. Choose your preferred framework below.

### Dapper

The Dapper integration automatically detects your database provider from the active `IDbConnection` and routes the generated SQL and `DynamicParameters` to Dapper's native execution methods.

```csharp
using SqlInterpol.Dapper;

public async Task<Product> GetProductAsync(IDbConnection connection, int id)
{
    using var db = connection.CreateSqlBuilder();
    db.Entity<Product>(out var p);

    var query = db.Append($"""
        SELECT * FROM {p} 
        WHERE {p.Id} = {id}
        """);

    return await connection.QuerySingleAsync<Product>(query);
}
```
👉 **Read the full [Dapper Integration Guide](integration-dapper.md)**

---

### Entity Framework Core

The EF Core integration inspects the `DbContext` provider to resolve the active SQL dialect, allowing you to use `FromSql` and `ExecuteSql` safely across different providers (like SQL Server, PostgreSQL, or SQLite InMemory).

```csharp
using SqlInterpol.EFCore;

public async Task<List<Product>> GetActiveProductsAsync(AppDbContext context)
{
    using var db = context.CreateSqlBuilder();
    db.Entity<Product>(out var p);

    var query = db.Append($"""
        SELECT * FROM {p} 
        WHERE {p.IsActive} = {true}
        """).Build();

    return await context.FromSql<Product>(query).ToListAsync();
}
```
👉 **Read the full [EF Core Integration Guide](integration-efcore.md)**

---

### Pure ADO.NET

For developers who prefer zero external ORM dependencies, the ADO.NET extensions automatically handle command creation, dialect detection, and native `DbParameter` mapping under the hood.

```csharp
using SqlInterpol.AdoNet;

public async Task UpdatePriceAsync(DbConnection connection, int id, decimal newPrice)
{
    using var db = connection.CreateSqlBuilder();
    db.Entity<Product>(out var p);

    var query = db.Append($"""
        UPDATE {p} 
        SET {p.Price} = {newPrice} 
        WHERE {p.Id} = {id}
        """).Build();

    // No manual DbCommand creation or parameter binding required!
    await connection.ExecuteNonQueryAsync(query);
}
```
👉 **Read the full [ADO.NET Integration Guide](integration-adonet.md)**