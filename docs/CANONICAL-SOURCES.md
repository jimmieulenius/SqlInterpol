# Canonical sources (code-work router)

**Purpose:** One screen: which doc owns which code-work task. Behavior lives in code, tests, and CI. Editor adapters only link here; they do not copy policy.

**Read order (code work):** [`AGENTS.md`](../AGENTS.md) → this file → **one** doc from [Task → one doc](#task--one-doc).

**Other intents:** [README.md](../README.md#start-here-by-intent).

## Fast path (read first)

- **Pick one task row** below — at most one owner doc per task.
- **Who wins when docs disagree:** [DOCUMENTATION-PRINCIPLES.md — Authority](DOCUMENTATION-PRINCIPLES.md#authority-when-docs-disagree).
- **This host:** SqlInterpol is a .NET **NuGet** library. Wiki-synced topic guides are **consumer** docs; this file + `docs/ops/` + kit meta are **contributor** situating ([AGENTS.md](../AGENTS.md#documentation-audience-nuget--wiki)).
- UI / auth rows are **n/a** (dated in [rank7-dated-queue.md](ops/rank7-dated-queue.md)).

## Core owners

| Concern | Owner |
|---------|--------|
| Product domain spine (install, quick path, doc index into feature specs) | [getting-started.md](getting-started.md) |
| Who we serve / Primary jobs | [customer-targets.md](product/heuristics/customer-targets.md) |
| Feature / claim fit | [customer-fit.md](product/heuristics/customer-fit.md) |
| Layering / module boundaries | [application-layering.md](engineering/architecture/application-layering.md) |
| Dedupe / hotspots / where things live / size and unused-export checks | [code-quality-and-refactor.md](ops/code-quality-and-refactor.md) |
| Governing priority ranks (higher wins) | [governing-priorities.md](ops/governing-priorities.md) |
| Enduring eng touch rules | [ongoing-engineering-bar.md](ops/ongoing-engineering-bar.md) |
| Tests when behavior moves (Rank 2) | [testing-bar.md](ops/testing-bar.md) |
| CI / merge commands | [ci-flow.md](ops/ci-flow.md) |
| Code-first / tribal → code | [CODE-FIRST.md](CODE-FIRST.md) |
| Doc structure and plain language | [DOCUMENTATION-PRINCIPLES.md](DOCUMENTATION-PRINCIPLES.md) |
| Integrations / hosting contracts | [integrations-as-code.md](ops/integrations-as-code.md) + [`infra/contracts/`](../infra/contracts/) |
| Auth / entitlement | **n/a** — library has no auth product surface ([rank7-dated-queue.md](ops/rank7-dated-queue.md)) |
| UI ranks / chrome / in-page layout | **n/a** — no product UI ([rank7-dated-queue.md](ops/rank7-dated-queue.md)); templates remain under `docs/product/heuristics/` for kit parity only |

## Task → one doc

| Task | Open (Fast path first) |
|------|------------------------|
| First-time product orientation / install | [getting-started.md](getting-started.md) |
| Change `SqlBuilder` / append / fragment / template / build API | [core-query-building.md](core-query-building.md) |
| Schema / entity / column mapping | [schema-mapping.md](schema-mapping.md) |
| Dialects / custom dialect extensibility | [extensibility-dialects.md](extensibility-dialects.md) |
| Pipeline rewriters | [pipeline-rewriters.md](pipeline-rewriters.md) |
| Dapper / EF Core / ADO.NET integration package | matching `docs/integration-*.md` |
| Analyzers / diagnostics | [analyzers.md](analyzers.md) |
| AOT / generators / performance | [performance-aot.md](performance-aot.md) |
| Add / change module boundary (public API vs pipeline vs dialect vs integration) | [application-layering.md](engineering/architecture/application-layering.md) |
| Structure / dedupe / size debt / unused exports | [code-quality-and-refactor.md](ops/code-quality-and-refactor.md) |
| Priority conflict (which owner vs which check vs depth) | [governing-priorities.md](ops/governing-priorities.md) |
| Side effects / leftover forks / clocks | [ongoing-engineering-bar.md](ops/ongoing-engineering-bar.md) |
| Unit / seam / e2e when behavior moves | [testing-bar.md](ops/testing-bar.md) |
| What CI to run / merge bar | [ci-flow.md](ops/ci-flow.md) |
| Doc ownership / dual homes | [DOCUMENTATION-PRINCIPLES.md](DOCUMENTATION-PRINCIPLES.md) |
| Should we build this (fit) | [customer-fit.md](product/heuristics/customer-fit.md) |
| Portal / deploy / release how stuck in wiki | [CODE-FIRST.md](CODE-FIRST.md) |
| Bootstrap / adopt status | [ops/adopt-receipt.md](ops/adopt-receipt.md) |
| Railkit apply story / token ledger | [ops/railkit-apply-journal.md](ops/railkit-apply-journal.md) |
