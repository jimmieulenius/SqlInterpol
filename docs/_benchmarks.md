# Benchmarks

All benchmarks measured on Windows 11 (10.0.26200.8457), .NET 8.0.27, X64 RyuJIT AVX2.

Run them yourself:
```bash
dotnet run --project src/SqlInterpol.Benchmarks -c Release
```

## Query Building (PostgreSQL)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| SimpleSelect | 4.742 μs | 1.00x | 7.71 KB |
| FilteredSelect | 7.704 μs | 1.62x | 10.79 KB |
| JoinQuery | 8.621 μs | 1.82x | 12.71 KB |
| ComplexJoinWithPaging | 19.841 μs | 4.18x | 28.25 KB |

## Same JOIN Query Across All Dialects

| Dialect | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| PostgreSql | 11.78 μs | 1.00x | 16.86 KB |
| MySql | 11.92 μs | 1.01x | 17.12 KB |
| SqLite | 11.98 μs | 1.02x | 16.97 KB |
| Oracle | 11.90 μs | 1.01x | 17.12 KB |
| SqlServer | 12.64 μs | 1.07x | 17.12 KB |

Cross-dialect rendering overhead is negligible — all dialects are within 7% of each other.

## Entity Metadata Lookup

| Method | Mean | Allocated |
|---|---:|---:|
| GenericLookup (`Cache<T>.Metadata`) | 0.706 ns | 0 B |
| RuntimeLookup (`ConcurrentDictionary`) | 5.326 ns | 0 B |
| AddEntityAndBuild (full round-trip) | 3.478 μs | 5.38 KB |

Metadata is zero-allocation once warm. The generic path (`Cache<T>` CLR static) is 7.5× faster than the runtime `Type`-based path.

## IN (...) Clause — Varying Collection Size

| Count | PostgreSql | SqlServer | MySql |
|---:|---:|---:|---:|
| 5 | 5.73 μs | 5.52 μs | 5.76 μs |
| 25 | 7.35 μs | 7.47 μs | 7.34 μs |
| 100 | 12.47 μs | 12.34 μs | 12.44 μs |

Collection expansion scales sub-linearly — going from 5 to 100 items adds only ~6.7 μs.

## Comparison vs Raw Strings and Dapper.SqlBuilder

### Scenario 1 — Simple filtered SELECT

| Method | Mean | Allocated | Notes |
|---|---:|---:|---|
| RawString (hardcoded) | ~0 ns | 0 B | String literal — JIT constant-folds it. No quoting, no params, no dialect. |
| DapperSqlBuilder | 260 ns | 1.6 KB | Template substitution only. Quoting and parameterization are your responsibility. |
| SqlInterpol (PostgreSQL) | 7.58 μs | 10.8 KB | Typed columns + auto-quoting + auto-parameterization + dialect rendering. |
| SqlInterpol (SQL Server) | 7.62 μs | 11.0 KB | Same source code as above — dialect switched at builder creation. |

### Scenario 2 — IN (...) clause with 10 values

| Method | Mean | Allocated | Notes |
|---|---:|---:|---|
| Raw manual loop | 73 ns | 792 B | Manually builds `@p0, @p1, ..., @p9` with a `StringBuilder` loop. |
| SqlInterpol | 7.30 μs | 12.0 KB | Pass the array directly — parameters expanded and registered automatically. |

### Scenario 3 — Aliased multi-table JOIN

| Method | Mean | Allocated | Notes |
|---|---:|---:|---|
| Raw hardcoded | ~0 ns | 0 B | Aliases, schema, and column names all hardcoded — breaks silently on rename. |
| SqlInterpol | 8.33 μs | 12.7 KB | Aliases declared once on the entity; all column refs resolve automatically. |

SqlInterpol's overhead is consistent at ~7–8 μs regardless of scenario — dominated by the SQL string rendering pass, not by entity resolution or parameterization. Raw string baselines show ~0 ns because the JIT constant-folds string literals; they perform no actual work at runtime.
