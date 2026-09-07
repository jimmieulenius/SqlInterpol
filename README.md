# ![logo][] SqlInterpol

![CI](https://github.com/jimmieulenius/SqlInterpol/actions/workflows/ci.yml/badge.svg) ![NuGet Version](https://img.shields.io/nuget/v/SqlInterpol?style=flat-square) ![NuGet Downloads](https://img.shields.io/nuget/dt/SqlInterpol?style=flat-square) ![License](https://img.shields.io/github/license/jimmieulenius/SqlInterpol?style=flat-square)

**SqlInterpol** is a next-generation, zero-boilerplate SQL Query Builder for .NET 8+.

It leverages C# 12 Interpolated String Handlers to let you write WYSIWYG (What You See Is What You Get) SQL queries with flawless type-safety, automatic parameterization, and cross-dialect SQL rendering.

Stop writing clumsy `SelectBuilder` chains. Write real SQL, and let C# do the heavy lifting.

> **Prerequisites:** .NET 8+ and C# 12+ (Visual Studio 2022 17.8+, JetBrains Rider 2023.3+, VS Code with C# Dev Kit, or `dotnet` CLI 8.0+).

[logo]: assets/sqlinterpol_64.svg?sanitize=true

## Features
* **WYSIWYG SQL:** The exact SQL structure you write is what executes — no mental translation between C# method chains and database queries.
* **Zero SQL Injection:** Every interpolated value is automatically converted to a native `DbParameter`. Raw identifiers require an explicit `Sql.Raw()` call, making injection points visible and auditable.
* **Type-Safe Schema:** Strongly-typed table and column references via C# property access (e.g., `{p.Price}`). Renamed a property? The compiler catches every broken reference instantly.
* **Full DML Support:** INSERT, UPDATE, DELETE, and UPSERT (`ON CONFLICT` / `ON DUPLICATE KEY UPDATE` / SQL Server `MERGE`) all work through the same interpolation model.
* **Dialect Agnostic:** Write once, run on SQL Server, PostgreSQL, MySQL, SQLite, Oracle, or Firebird. Dialect-specific syntax differences are rewritten at render time.
* **Pre-Compiled Templates & CRUD Caching:** Compile hot queries into `ISqlTemplate` instances once for O(1) parameter binding and zero-allocation execution paths.
* **Composable Subqueries:** A captured `ISqlQuery` can be interpolated directly into another query as a subquery — compose complex nested SQL from typed, reusable query variables.
* **Compile-Time Safety:** Bundled Roslyn Analyzers catch injection attempts, unsupported dialect features, and invalid selectors *while you type*.
* **Native Integrations:** Drop-in support for **Dapper**, **Entity Framework Core**, and pure **ADO.NET**.
* **High-Performance, Zero-Allocation Parsing:** Built on `Span<char>`, `ArrayPool<T>`, and Roslyn Interceptors for full Native AOT compatibility.

## Installation

**Using .NET CLI:**
```bash
dotnet add package SqlInterpol
dotnet add package SqlInterpol.Dapper              # If you use Dapper
dotnet add package SqlInterpol.EntityFrameworkCore # If you use EF Core
```

**Using Package Manager (PowerShell):**
```powershell
Install-Package SqlInterpol
Install-Package SqlInterpol.Dapper
Install-Package SqlInterpol.EntityFrameworkCore
```

## Quick Start (with Dapper)

The syntax is entirely driven by standard C# string interpolation. Native SQL comments are fully supported and safely ignored by the parameterization engine!

```csharp
using SqlInterpol.Dapper;

public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync(IDbConnection dbConnection, int minPrice)
{
    // 1. Automatically maps the correct SQL dialect (PostgreSQL, SQL Server, etc.) based on the connection!
    using var db = dbConnection.CreateSqlBuilder();

    // 2. Register scoped entities for type-safe property access
    db.Entity<Product>(out var p)
      .Entity<Category>(out var c);

    // 3. Write highly readable, type-safe SQL — joins, aliases, parameters, all in one place
    var query = db.Append($"""
        -- We can use standard SQL comments anywhere!
        SELECT 
            {p.Id}, 
            {p.Name},
            {p.Price},
            {c.Name} AS CategoryName
        FROM {p} AS prod
        JOIN {c} AS cat
            ON {p.CategoryId} = {c.Id}
        WHERE {p.IsActive} = {true}
            AND {p.Price} > {minPrice} /* minPrice is parameterized */
        ORDER BY {p.Name}
        """).Build();

    // 4. Execute using native Dapper methods!
    return await dbConnection.QueryAsync<ProductDto>(query);
}
```

### What actually happens under the hood?
Because SqlInterpol builds a typed segment pipeline, the query above is rendered into perfectly formatted, dialect-specific SQL, and `minPrice` is safely extracted as a parameter.

**Generated SQL (PostgreSQL):**
```sql
-- We can use standard SQL comments anywhere!
SELECT 
    prod."Id", 
    prod."Name",
    prod."Price",
    cat."Name" AS CategoryName
FROM "Products" AS prod
JOIN "Categories" AS cat
    ON prod."CategoryId" = cat."Id"
WHERE prod."IsActive" = $1
    AND prod."Price" > $2 /* minPrice is parameterized */
ORDER BY prod."Name"
```

### Effortless Inserts

No more mapping parameters one-by-one. Use the built-in, globally cached CRUD helpers to generate dialect-optimized statements instantly:

```csharp
using var db = dbConnection.CreateSqlBuilder();
db.Entity<Product>(out var p);

var newProduct = new Product { Name = "Mechanical Keyboard", Price = 120m };

// Fetches a cached INSERT template and binds parameters with zero allocation
var query = db.AppendInsert(p, newProduct).Build();
```

**Generated SQL (PostgreSQL):**
```sql
INSERT INTO "Products" ("Name", "Price")
VALUES ($1, $2)
```

The same pattern works for bulk `INSERT`, `AppendUpdate`, `AppendDelete`, and cross-dialect `AppendUpsert` — all with zero parameter wiring.

## Documentation

| Topic | Description | Link |
|---|---|---|
| **Getting Started** | Basic setup, DI configuration, and Quick Start guides | [`docs/getting-started.md`](docs/getting-started.md) |
| **Core Query Building** | The `SqlBuilder` API, `Sql` helpers, CTEs, and standard DML | [`docs/core-query-building.md`](docs/core-query-building.md) |
| **Schema & Entity Mapping** | Attribute mapping (`[SqlTable]`), auto-aliasing, and `AS` syntax | [`docs/schema-mapping.md`](docs/schema-mapping.md) |
| **Dynamic Queries** | Conditional appending, dynamic collections (`IN`), and subqueries | [`docs/dynamic-queries.md`](docs/dynamic-queries.md) |
| **Cross-Dialect Transpilation** | Auto-translating Paging, Upserts, Locks, and SET operations | [`docs/cross-dialect-transpilation.md`](docs/cross-dialect-transpilation.md) |
| **Query Templates & Caching** | Pre-compiled queries and `SqlCrudTemplateCache` | [`docs/templates-caching.md`](docs/templates-caching.md) |
| **Performance & AOT** | Compile-time routing, source generators, and benchmarks | [`docs/performance-aot.md`](docs/performance-aot.md) |
| **Testing & Mocking** | Unit testing, `SqlTestCase`, and specification-driven testing | [`docs/testing-mocking.md`](docs/testing-mocking.md) |
| **Extensibility & Dialects** | Building custom dialects, fragments, and pipeline extensions | [`docs/extensibility-dialects.md`](docs/extensibility-dialects.md) |
| **Compiler Pipeline** | Preprocessors, semantic tags, rewriters, and rendering mechanics | [`docs/pipeline-rewriters.md`](docs/pipeline-rewriters.md) |
| **Configuration** | Global options, formatters, and telemetry (`SqlInterpolOptions`) | [`docs/configuration-options.md`](docs/configuration-options.md) |
| **Roslyn Analyzers** | Compile-time diagnostics (`SQLIA01`–`SQLIA07`), severity, and safety | [`docs/analyzers.md`](docs/analyzers.md) |
| **Dapper Integration** | Seamless multi-mapping and execution with Dapper | [`docs/integration-dapper.md`](docs/integration-dapper.md) |
| **EF Core Integration** | Using `FromSql`, `ExecuteSql`, and sharing schema mappings | [`docs/integration-entityframeworkcore.md`](docs/integration-entityframeworkcore.md) |
| **ADO.NET Integration** | Pure zero-boilerplate `DbCommand` and parameter execution | [`docs/integration-adonet.md`](docs/integration-adonet.md) |

## Contributing
Contributions are welcome! Please open an issue before submitting large pull requests to discuss proposed changes.

## License
MIT License. See [LICENSE](LICENSE) for details.