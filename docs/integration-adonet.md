# Pure ADO.NET Integration

The `SqlInterpol` core package provides zero-boilerplate execution methods for raw ADO.NET, completely eliminating manual `DbCommand` instantiation and parameter mapping.

## Installation

```bash
dotnet add package SqlInterpol
```

## Query Execution

The library extends `DbConnection` to accept `SqlQueryResult` objects directly. Under the hood, the engine securely binds your interpolated arguments to native `DbParameter` instances.

### Reading Data (`DbDataReader`)

Execute queries and iterate over results using standard reader patterns without any manual command or parameter setup:

```csharp
using SqlInterpol.AdoNet;

public async Task<List<Product>> GetProductsAsync(DbConnection connection, decimal minPrice)
{
    using var db = connection.CreateSqlBuilder();
    db.Entity<Product>(out var p);

    var query = db.Append($"""
        SELECT {p.Id}, {p.Name}, {p.Price}
        FROM {p}
        WHERE {p.Price} >= {minPrice}
        """).Build();

    using var reader = await connection.ExecuteReaderAsync(query);
    var products = new List<Product>();

    while (await reader.ReadAsync())
    {
        products.Add(new Product
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Price = reader.GetDecimal(2)
        });
    }

    return products;
}
```

### Executing Non-Queries & Scalars

For updates, inserts, deletes, and aggregate queries:

```csharp
// Executing Non-Queries
var updateQuery = db.Append($"""
    UPDATE {p} 
    SET {p.Price} = {newPrice} 
    WHERE {p.Id} = {id}
    """).Build();

await connection.ExecuteNonQueryAsync(updateQuery);

// Executing Scalar Queries
var countQuery = db.Append($"SELECT COUNT(*) FROM {p}").Build();
var totalProducts = (long)(await connection.ExecuteScalarAsync(countQuery) ?? 0L);
```

**Supported Direct Extensions:**
*   `ExecuteNonQueryAsync` / `ExecuteNonQuery`
*   `ExecuteScalarAsync` / `ExecuteScalar`
*   `ExecuteReaderAsync` / `ExecuteReader`

> ℹ️ **Transactions & Command Behaviors**  
> All overloads accept standard ADO.NET arguments like `DbTransaction`, `CancellationToken`, and `CommandBehavior` (e.g., `CommandBehavior.CloseConnection`).

## Existing Command Execution

If you need fine-grained control over the command lifecycle (such as setting custom timeouts or command types), you can execute the query directly on an existing `DbCommand`.

```csharp
using var command = connection.CreateCommand();
command.CommandTimeout = 120;

var query = db.Append($"DELETE FROM Logs WHERE CreatedAt < {DateTime.UtcNow.AddDays(-30)}").Build();

// Applies CommandText and adds DbParameters directly to the command instance
var rowsAffected = await command.ExecuteNonQueryAsync(query);
```

## Dialect Auto-Detection

When you call `connection.CreateSqlBuilder()`, the engine walks the `IDbConnection` type hierarchy to automatically resolve the correct database dialect (e.g., `SqlConnection` resolves to SQL Server, `NpgsqlConnection` to PostgreSQL, `SqliteConnection` to SQLite). 

If you use connection-wrapping middleware that masks the underlying connection type, you can globally register a custom resolver:

```csharp
SqlInterpolAdoNetExtensions.ConnectionResolvers.Add(type => 
    type.Name == "ProfiledDbConnection" ? new SqlServerDialect() : null);
```