# Product backlog notes

Operational Rank 5 hygiene: keep this list honest against shipped code. Prefer CANONICAL task rows + tests over undated bullets.

## Open

* Fix SqlTemplate after new simplified syntax — needs a repro / failing characterization before code change ([templates-caching.md](docs/templates-caching.md))

## Done / retired

* Add AppendUpsert — shipped (`SqlBuilderExtensions.AppendUpsert` + `IUpsertTestSuite` / `UpsertTemplateData`)
* Unit tests for templates and Append… methods — template suites exist; `Template_AppendLine_Select` covers `AppendLine(ISqlTemplate)` via Spec
* EntityAutoAliasing = true by default — rejected (violates WYSIWYG)
* Characterize AOT×CrossDialect handwritten UPSERT — retired into Spec + AOT call-site
* AOT-intercept handwritten UPSERT/ON CONFLICT — shipped 2026-10-02 (structural GetSegment emit; CrossDialect at `Build()`; proof: `UpsertTestSuite` + `AotUpsertCallSiteTests`). Consumer note: [performance-aot.md](docs/performance-aot.md)
