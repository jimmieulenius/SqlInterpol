# Performance & AOT

`SqlInterpol` is engineered for high-throughput, zero-allocation query building. By leveraging modern C# features like `[InterpolatedStringHandler]` and Roslyn Interceptors, the engine shifts the heavy lifting of structural parsing and dialect transpilation from runtime to compile time.

---

## Ahead-Of-Time (AOT) Compilation

For applications running on .NET 8 and .NET 9+, `SqlInterpol` automatically opts into compiler interceptors. This completely eliminates runtime string parsing overhead.

*   **Zero-Allocation Handlers:** The `SqlQueryInterpolatedStringHandler` uses an `ArrayPool<PendingHole>` to capture SQL text literals and typed interpolation holes without triggering per-hole heap allocations.
*   **Compile-Time Routing:** The source generator maps your C# interpolated strings directly to highly optimized structural segments. The query bypasses the JIT-evaluation path entirely.
*   **Telemetry & Validation:** The `SqlBuilder` exposes an `IsAotIntercepted` flag to verify successful compile-time routing. The `SQLIA07` analyzer (see [Analyzer Reference](analyzers.md)) warns when `Template()` is called on a non-static path. 

To detect whether a specific build was intercepted, read `db.LastBuildWasAotIntercepted` after calling `Build()`, or call `db.AssertAotIntercepted()` from the `SqlInterpol.Testing.Xunit` package in test code.

```csharp
var result = db.Append($"SELECT {p.Id} FROM {p}").Build();
if (!db.LastBuildWasAotIntercepted)
{
    // Log a warning — the interpolated string was not recognized by the generator
}
```

---

## Pre-Compiled Templates

If you have a complex, highly dynamic query that executes frequently, you can compile it into a reusable `ISqlTemplate`. 

Templates run through the `SqlPipeline` (including preprocessors, rewriters, and renderers) exactly once. The resulting `ISqlTemplate` bypasses stream processing during execution, natively injecting arguments directly into the finalized SQL string in O(1) time.

```csharp
// 1. Compile the template once (e.g., during startup or static initialization)
var builder = new SqlBuilder();
builder.Entity<OrderModel>(out var o);

var template = builder.Template($"""
    SELECT {o.Id}, {o.Status} 
    FROM {o} 
    WHERE {o.CustomerId} = {Sql.Arg("TenantId")} 
      AND {o.Status} = {Sql.Arg("Status")}
    """);

// 2. Execute the cached template in O(1) time anywhere in your app
var executeBuilder = new SqlBuilder();
var result = executeBuilder.Append(template, new { TenantId = 42, Status = "Active" }).Build();
```

---

## Globally Cached CRUD Operations

To maximize performance for standard database operations, `SqlInterpol` ships with natively cached CRUD extension methods. 

Methods like `AppendInsert`, `AppendUpdate`, `AppendUpsert`, and `AppendDelete` utilize the `SqlCrudTemplateCache`. When you call one of these methods, the engine generates the optimal SQL string for the active dialect once, caches it globally, and injects parameters instantly for all subsequent calls.

```csharp
using SqlInterpol;

var db = new SqlBuilder();
db.Entity<Product>(out var p);

var newProduct = new Product { Name = "Widget", Price = 9.99m };

// Fetches a highly-optimized, dialect-specific INSERT template from the global cache
db.AppendInsert(p, newProduct);

var result = db.Build();
```

For UPSERT operations, the caching engine natively handles the dialect-specific transpilation (e.g., rendering a SQL Server `MERGE` statement versus a PostgreSQL `INSERT ... ON CONFLICT DO UPDATE`) before caching the template, ensuring cross-database compatibility with zero runtime penalty.

---

## Expected Throughput

All benchmarks measured on Windows 11 (.NET 8, X64 RyuJIT AVX2). Run them yourself:
```bash
dotnet run --project benchmarks/SqlInterpol.Benchmarks -c Release
```

### Query Building (PostgreSQL)

| Method | Mean | Ratio | Allocated |
| :--- | ---: | ---: | ---: |
| `SimpleSelect` | 4.742 μs | 1.00× | 7.71 KB |
| `FilteredSelect` | 7.704 μs | 1.62× | 10.79 KB |
| `JoinQuery` | 8.621 μs | 1.82× | 12.71 KB |
| `ComplexJoinWithPaging` | 19.841 μs | 4.18× | 28.25 KB |

### Same JOIN Query Across All Dialects

| Dialect | Mean | Ratio | Allocated |
| :--- | ---: | ---: | ---: |
| PostgreSQL | 11.78 μs | 1.00× | 16.86 KB |
| MySQL | 11.92 μs | 1.01× | 17.12 KB |
| SQLite | 11.98 μs | 1.02× | 16.97 KB |
| Oracle | 11.90 μs | 1.01× | 17.12 KB |
| SQL Server | 12.64 μs | 1.07× | 17.12 KB |

Cross-dialect rendering overhead is negligible — all dialects are within 7% of each other.

### Entity Metadata Lookup

| Method | Mean | Allocated |
| :--- | ---: | ---: |
| Generic lookup (`Cache<T>.Metadata`) | 0.706 ns | 0 B |
| Runtime lookup (`ConcurrentDictionary`) | 5.326 ns | 0 B |
| `AddEntityAndBuild` (full round-trip) | 3.478 μs | 5.38 KB |

Metadata is zero-allocation once warm. The generic CLR-static path is 7.5× faster than the `Type`-keyed dictionary path.

### `IN (...)` Clause — Varying Collection Size

| Count | PostgreSQL | SQL Server | MySQL |
| ---: | ---: | ---: | ---: |
| 5 | 5.73 μs | 5.52 μs | 5.76 μs |
| 25 | 7.35 μs | 7.47 μs | 7.34 μs |
| 100 | 12.47 μs | 12.34 μs | 12.44 μs |

Going from 5 to 100 items adds only ~6.7 μs — collection expansion scales sub-linearly.

### Comparison vs Raw Strings and Dapper.SqlBuilder

| Method | Mean | Allocated | Notes |
| :--- | ---: | ---: | :--- |
| Raw string literal | ~0 ns | 0 B | JIT constant-folds it. No quoting, no params, no dialect. |
| `DapperSqlBuilder` | 260 ns | 1.6 KB | Template substitution only — quoting/params are manual. |
| `SqlInterpol` (PostgreSQL) | 7.58 μs | 10.8 KB | Typed columns + auto-quoting + auto-params + dialect. |
| `SqlInterpol` (SQL Server) | 7.62 μs | 11.0 KB | Same source — dialect switched at builder creation. |

`SqlInterpol`'s overhead is consistent at ~7–8 μs regardless of query shape, dominated by the SQL string rendering pass, not by entity resolution or parameterization.