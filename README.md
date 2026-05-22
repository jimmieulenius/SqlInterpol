# SQL Interpolation

<p align="center">
  <img src="docs/Images/SQLI_logo.svg" alt="SqlInterpol" width="200"/>
</p>

![NuGet Version](https://img.shields.io/nuget/v/SqlInterpol?style=flat-square) ![NuGet Downloads](https://img.shields.io/nuget/dt/SqlInterpol?style=flat-square) ![License](https://img.shields.io/github/license/jimmieulenius/SqlInterpol?style=flat-square)

**SQL Interpolation** (SQLI) is a next-generation, zero-boilerplate SQL Query Builder for .NET 8+.

It leverages C# 12 Interpolated String Handlers to let you write WYSIWYG (What You See Is What You Get) SQL queries with flawless type-safety, automatic parameterization, and cross-dialect SQL rendering.

Stop writing clumsy `SelectBuilder` chains. Write real SQL, and let C# do the heavy lifting.

> **Prerequisites:** .NET 8+ and C# 12+ (Visual Studio 2022 17.8+, JetBrains Rider 2023.3+, VS Code with C# Dev Kit, or `dotnet` CLI 8.0+).

## Table of Contents
- [SQL Interpolation](#sql-interpolation)
  - [Table of Contents](#table-of-contents)
  - [Features](#features)
  - [Installation](#installation)
  - [Quick Start (with Dapper)](#quick-start-with-dapper)
    - [What actually happens under the hood?](#what-actually-happens-under-the-hood)
  - [Documentation](#documentation)
  - [Contributing](#contributing)
  - [License](#license)

## Features
* **WYSIWYG SQL:** The exact SQL structure you write is what executes — no mental translation between C# method chains and database queries.
* **Zero SQL Injection:** Every interpolated value is automatically converted to a native `DbParameter`. Raw identifiers require an explicit `Sql.Raw()` call, making injection points visible and auditable.
* **Type-Safe Schema:** Strongly-typed table and column references via C# expressions (e.g., `{{p[x => x.Price]}}`). Renamed a property? The compiler catches every broken reference instantly.
* **Full DML Support:** INSERT, UPDATE, DELETE, and UPSERT (`ON CONFLICT` / `ON DUPLICATE KEY UPDATE` / SQL Server `MERGE`) all work through the same interpolation model.
* **Dialect Agnostic:** Write once, run on SQL Server, PostgreSQL, MySQL, SQLite, Oracle, or Firebird. Dialect-specific syntax differences are rewritten at render time.
* **Composable Subqueries:** A built `SqlQuery` can be interpolated directly into another query as a subquery — compose complex nested SQL from typed, reusable query variables.
* **Compile-Time Safety:** Bundled Roslyn Analyzers catch injection attempts, unsupported dialect features, and invalid selectors *while you type*.
* **Native Integrations:** Drop-in support for **Dapper** and **Entity Framework Core**.

## Installation

**Using .NET CLI:**
```bash
dotnet add package SqlInterpol
dotnet add package SqlInterpol.Dapper               # If you use Dapper
dotnet add package SqlInterpol.EntityFrameworkCore  # If you use EF Core
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
public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync(IDbConnection dbConnection, int minPrice)
{
    // 1. Automatically maps the correct SQL dialect (Postgres, SQL Server, etc.) based on the connection!
    var db = dbConnection.CreateSqlBuilder();

    // 2. Write highly readable, type-safe SQL — joins, aliases, parameters, all in one place
    var query = db.Query<Product, Category>((p, c) => db.Append($$"""
        -- We can use standard SQL comments anywhere!
        SELECT 
            {{p[x => x.Id]}}, 
            {{p[x => x.Name]}},
            {{p[x => x.Price]}},
            {{c[x => x.Name]}} AS CategoryName
        FROM {{p}} AS prod
        JOIN {{c}} AS cat
            ON {{p[x => x.CategoryId]}} = {{c[x => x.Id]}}
        WHERE {{p[x => x.IsActive]}} = {{true}}
            AND {{p[x => x.Price]}} > {{minPrice}} /* minPrice is parameterized */
        ORDER BY {{p[x => x.Name]}}
    """)).Build();

    // 3. Execute using native Dapper methods!
    return await dbConnection.QueryAsync<ProductDto>(query);
}
```

### What actually happens under the hood?
Because SQLI builds a typed segment pipeline, the query above is rendered into perfectly formatted, dialect-specific SQL, and `minPrice` is safely extracted as a parameter.

**Generated SQL (PostgreSQL):**
```sql
-- We can use standard SQL comments anywhere!
SELECT 
    prod."Id", 
    prod."Name",
    prod."Price",
    cat."Name" AS CategoryName
FROM "Product" AS prod
JOIN "Category" AS cat
    ON prod."CategoryId" = cat."Id"
WHERE prod."IsActive" = $1
    AND prod."Price" > $2 /* minPrice is parameterized */
ORDER BY prod."Name"
```

## Documentation

| Topic | Description |
|---|---|
| [DML — INSERT, UPDATE, DELETE, UPSERT](docs/dml.md) | Full mutation examples with generated SQL for all dialects |
| [How It Works](docs/how-it-works.md) | IN clause expansion, composable subqueries, dynamic SQL, alias resolution, name mapping, DI setup |
| [ADO.NET](docs/ado-net.md) | Using `SqlQueryResult` directly with `DbCommand` |
| [Entity Framework Core](docs/efcore.md) | `FromSql` integration, `Include()`, and `DbContext` usage |
| [Roslyn Analyzers](docs/analyzers.md) | Compile-time diagnostics, severity levels, and suppression |
| [Dialect Cheat-Sheet](docs/dialect-cheatsheet.md) | Full cross-dialect reference: `LIMIT`, `FOR UPDATE`, `RETURNING`, `ON CONFLICT`, and more |
| [Benchmarks](docs/benchmarks.md) | Detailed performance tables across scenarios and dialects |

## Contributing
Contributions are welcome! Please open an issue before submitting large pull requests to discuss proposed changes.

## License
MIT License. See [LICENSE](LICENSE) for details.