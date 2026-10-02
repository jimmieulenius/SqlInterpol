# Application layering

**Purpose:** Stack-neutral layer boundaries so locks, caching, security, and agent edits stay consistent.

## Fast path (read first)

- **Thin shells:** Integration packages (Dapper / EF / ADO.NET) and public extension methods parse/adapt and call core. No dialect or pipeline logic owned only in an integration package.
- **Domain owns writes:** Core `SqlInterpol` owns segment pipeline, metadata, and render. Generators/analyzers own compile-time surfaces.
- **Hot paths first:** `Build` / render / template bind stay allocation-conscious; rare diagnostics stay off the hot path.
- **Single source of truth:** One module per concern (dialect, rewriter, metadata registry); surfaces call it.
- **Quality detail:** [code-quality-and-refactor.md](../../ops/code-quality-and-refactor.md).

## Boundary table (SqlInterpol)

| Layer | Location | Responsibility |
|-------|----------|----------------|
| **Public API** | `src/SqlInterpol/SqlBuilder*.cs`, `SqlBuilderExtensions.*` | User-facing builder, CRUD helpers, dialect factories |
| **Execution / results** | `src/SqlInterpol/Execution/` | Query results, templates, parameter bags |
| **Pipeline** | `src/SqlInterpol/Pipeline/` | Preprocess, rewrite, feature gates |
| **Segments** | `src/SqlInterpol/Segments/` | AST-like segment types and fragments |
| **Schema / metadata** | `src/SqlInterpol/Schema/` | Entity/column metadata, registry |
| **Dialects** | `src/SqlInterpol/Dialects/` | Render rules and supported features per engine |
| **Configuration** | `src/SqlInterpol/Configuration/` | Options, enum format, layout |
| **Integrations (thin)** | `src/SqlInterpol.Dapper`, `.EntityFrameworkCore`, `AdoNet/` | Map core results onto host data APIs |
| **Compile-time** | `src/SqlInterpol.Generators`, `.Analyzers` | AOT interceptors, diagnostics |
| **Test harness** | `src/SqlInterpol.Testing.*`, `tests/` | Specs, shared suites, e2e |

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| New persistence of SQL shape in core pipeline/dialect modules | New dialect forks inside Dapper/EF packages |
| One write path per concern (e.g. one feature-gate helper) | Parallel validation loops copied into Build and generators without a shared owner |
| Batch / pooled work on hot render paths | N+1 metadata reflection on every bind when registry already caches |
| Fail-fast unsupported dialect features at build | Silent downgrade of SQL meaning |
| Integration packages as adapters only | Business/query semantics that exist only in an integration project |

## Host fill-in

Folder map above is the SqlInterpol specialize. When adding a concern, name its layer in the same change and update [CANONICAL-SOURCES.md](../../CANONICAL-SOURCES.md) if a new task row is needed.
