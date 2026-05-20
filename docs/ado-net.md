# ADO.NET Usage

SqlInterpol works with any ADO.NET provider — no additional package needed. Build your query as normal and wire the `SqlQueryResult` to a `DbCommand` manually.

## Basic Usage

```csharp
var db = SqlBuilder.PostgreSql();
decimal maxPrice = 100m;

var result = db.Query<Product>(p => db.Append($$"""
    SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
    FROM {{p}}
    WHERE {{p[x => x.Price]}} < {{maxPrice}}
    """)).Build();

using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

using var cmd = CreateCommand(connection, result);
using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    // map columns...
}
```

**Generated SQL (PostgreSQL):**
```sql
SELECT "Product"."Id", "Product"."Name"
FROM "Product"
WHERE "Product"."Price" < $1
```

`cmd.CreateParameter()` creates the correct provider-specific parameter type for the connection (`NpgsqlParameter`, `SqlParameter`, etc.) — no casting needed.

## Execute (INSERT / UPDATE / DELETE)

```csharp
var patch = new { Status = "Shipped", Total = 99.99m };
int orderId = 42;

var result = db.Query<Order>(o => db.Append($$"""
    UPDATE {{o}}
    SET {{patch}}
    WHERE {{o[x => x.Id]}} = {{orderId}}
    """)).Build();

using var cmd = CreateCommand(connection, result);
int rowsAffected = await cmd.ExecuteNonQueryAsync();
```

## Tip: Extract a Helper

If you use ADO.NET throughout the project, a small local helper avoids repetition:

```csharp
static DbCommand CreateCommand(DbConnection connection, SqlQueryResult result)
{
    var cmd = connection.CreateCommand();
    cmd.CommandText = result.Sql;
    foreach (var (name, value) in result.Parameters)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }
    return cmd;
}
```

## Auto-Detect Dialect from IDbConnection

Rather than hardcoding the dialect, use the `IDbConnection` extension to detect it automatically from the connection type:

```csharp
using var connection = new NpgsqlConnection(connectionString);
var db = connection.CreateSqlBuilder(); // Detects PostgreSQL automatically
```
