# Product backlog notes

Operational Rank 5 hygiene: keep this list honest against shipped code. Prefer CANONICAL task rows + tests over undated bullets.

## Open

* Fix SqlTemplate after new simplified syntax — needs a repro / failing characterization before code change ([templates-caching.md](docs/templates-caching.md))
* Honor CrossDialectSqlTranspilation in AOT (for example for UPSERT) — [performance-aot.md](docs/performance-aot.md), [cross-dialect-transpilation.md](docs/cross-dialect-transpilation.md); due clock also in [docs/ops/rank7-dated-queue.md](docs/ops/rank7-dated-queue.md) if elevated to ideal instrument

## Done / retired

* Add AppendUpsert — shipped (`SqlBuilderExtensions.AppendUpsert` + `IUpsertTestSuite` / `UpsertTemplateData`)
* Unit tests for templates and Append… methods — template suites already cover Append/CRUD templates; further AppendLine(ISqlTemplate) seam tests optional later
* EntityAutoAliasing = true by default — rejected (violates WYSIWYG)
