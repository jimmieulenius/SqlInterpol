# Entity Framework Core Integration

SqlInterpol integrates with EF Core via the `SqlInterpol.EntityFrameworkCore` package, letting you use raw SQL while keeping full entity tracking, `Include()`, and LINQ composition.

## Setup

```bash
dotnet add package SqlInterpol.EntityFrameworkCore
```

## Usage

```csharp
var db = _dbContext.CreateSqlBuilder(); // Auto-detects the EF Core provider dialect!

var query = db.Query<Product>(p => db.Append($$"""
    SELECT
        {{p[x => x.Id]}},
        {{p[x => x.Name]}},
        {{p[x => x.CategoryId]}}
    FROM {{p}}
    WHERE {{p[x => x.CategoryId]}} = {{5}}
""")).Build();

// Compose EF LINQ on top of the raw SQL — including navigation properties!
var products = await _dbContext.Products
    .FromSql(query)
    .Include(x => x.Category)
    .ToListAsync();
```

`CreateSqlBuilder()` reads the current EF Core database provider and automatically selects the matching SqlInterpol dialect (SQL Server, PostgreSQL, SQLite, etc.) — no manual dialect configuration needed.

## When to Use SqlInterpol with EF Core

| Use case | Recommended approach |
|---|---|
| Simple CRUD on a single entity | EF Core LINQ |
| Complex multi-table queries with filtering, paging, aggregates | SqlInterpol + `FromSql` |
| Bulk INSERT / UPDATE / DELETE without loading entities | SqlInterpol standalone |
| Raw SQL with entity tracking and `Include()` | SqlInterpol + `FromSql` |

## SqlQueryResult Extensions

The `SqlInterpol.EntityFrameworkCore` package also provides extension methods on `SqlQueryResult` for direct use with `DbContext.Database`:

```csharp
var result = db.Query<Order>(o => db.Append($$"""
    UPDATE {{o}}
    SET {{patch}}
    WHERE {{o[x => x.Id]}} = {{orderId}}
    """)).Build();

await _dbContext.Database.ExecuteSqlRawAsync(result.Sql, result.Parameters.ToArray());
```
