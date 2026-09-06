# Performance & AOT

`SqlInterpol` is engineered for high-throughput, zero-allocation query building. By leveraging modern C# features like `[InterpolatedStringHandler]` and Roslyn Interceptors, the engine shifts the heavy lifting of structural parsing and dialect transpilation from runtime to compile time.

---

## Ahead-Of-Time (AOT) Compilation

For applications running on .NET 8 and .NET 9+, `SqlInterpol` automatically opts into compiler interceptors. This completely eliminates runtime string parsing overhead.

*   **Zero-Allocation Handlers:** The `SqlQueryInterpolatedStringHandler` uses an `ArrayPool<PendingHole>` to capture SQL text literals and typed interpolation holes without triggering per-hole heap allocations.
*   **Compile-Time Routing:** The source generator maps your C# interpolated strings directly to highly optimized structural segments. The query bypasses the JIT-evaluation path entirely.
*   **Telemetry & Validation:** The `SqlBuilder` exposes an `IsAotIntercepted` flag to verify successful compile-time routing. 

You can strictly enforce AOT compilation globally to prevent hidden performance regressions:

```csharp
using SqlInterpol.Configuration;

SqlInterpolOptions.Default = new SqlInterpolOptions
{
    // Throws an exception if a query falls back to dynamic JIT compilation
    RequireAotCompilation = true 
};
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