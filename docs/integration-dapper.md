# Dapper Integration

The `SqlInterpol.Dapper` package bridges type-safe AST generation with Dapper's high-performance materializer, eliminating manual parameter mapping and raw string concatenation.

## Installation

```bash
dotnet add package SqlInterpol.Dapper
```

## Query Execution

The package extends `IDbConnection` to accept `SqlQueryResult` objects directly. Under the hood, the engine translates your interpolated arguments into Dapper's `DynamicParameters`, ensuring zero-friction execution.

```csharp
using SqlInterpol.Dapper;

public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(IDbConnection db, int categoryId)
{
    using var builder = db.CreateSqlBuilder();
    builder.Entity<Product>(out var p);

    var query = builder.Append($"""
        SELECT * FROM {p}
        WHERE {p.CategoryId} = {categoryId}
        ORDER BY {p.Name}
        """).Build();

    // Routes the SQL and DynamicParameters natively to Dapper
    return await db.QueryAsync<Product>(query);
}
```

**Supported Direct Extensions:**
*   `QueryAsync<T>` / `Query<T>`
*   `QueryFirstAsync<T>` / `QueryFirst<T>`
*   `QueryFirstOrDefaultAsync<T>` / `QueryFirstOrDefault<T>`
*   `QuerySingleAsync<T>` / `QuerySingle<T>`
*   `QuerySingleOrDefaultAsync<T>` / `QuerySingleOrDefault<T>`
*   `ExecuteAsync` / `Execute`
*   `ExecuteScalarAsync<T>` / `ExecuteScalar<T>`

> ℹ️ **Transactions & Timeouts**  
> All overloads accept standard Dapper arguments like `IDbTransaction`, `commandTimeout`, and `cancellationToken`.

## Advanced: Multi-Mapping & Grid Reader

For complex Dapper workflows not directly wrapped by the extensions (such as Multi-Mapping or `QueryMultiple`), you can manually extract the SQL string and the generated `DynamicParameters`.

```csharp
using var builder = db.CreateSqlBuilder();
builder.Entity<Order>(out var o)
       .Entity<User>(out var u);

var query = builder.Append($"""
    SELECT {o.Id}, {o.Total}, {u.Id}, {u.Name}
    FROM {o}
    INNER JOIN {u} ON {o.UserId} = {u.Id}
    WHERE {o.Status} = {"Shipped"}
    """).Build();

// Extract components for native Dapper Multi-Mapping
var dapperParams = query.ToDynamicParameters();

var orders = await db.QueryAsync<Order, User, Order>(
    sql: query.Sql,
    map: (order, user) => 
    {
        order.User = user;
        return order;
    },
    param: dapperParams,
    splitOn: "Id"
);
```

## Dialect Auto-Detection

When you call `connection.CreateSqlBuilder()`, the engine walks the `IDbConnection` type hierarchy to automatically resolve the correct database dialect (e.g., `SqlConnection` resolves to SQL Server, `NpgsqlConnection` to PostgreSQL). 

If you use connection-wrapping middleware (like MiniProfiler or OpenTelemetry) that masks the underlying connection type, you can globally register a custom resolver:

```csharp
SqlInterpolDapperExtensions.ConnectionResolvers.Add(type => 
    type.Name == "ProfiledDbConnection" ? new SqlServerDialect() : null);
```