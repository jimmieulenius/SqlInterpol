# Adopt receipt — SqlInterpol + Railkit

**Purpose:** In-repo anchor for adopt progress and usefulness decision. Update this file as packages complete (do not rely on chat alone).

## Fast path (read first)

- Packages table below is the status truth for this adopt.
- Yardstick: [ADOPTION.md](../ADOPTION.md) exit checks + host success criteria in this file.
- Usefulness verdict is at the bottom of this file.
- **Story + token ledger:** [railkit-apply-journal.md](railkit-apply-journal.md).
- **Upstream PR scope:** LLM/agent situating rails only — **no SqlInterpol product behavior change** in the contribution branch.
- **Doc audience:** wiki allowlist + consumer **At a glance** (no situating leaks). Kit handover: [kit-feedback-doc-audience.md](kit-feedback-doc-audience.md).

**Host:** SqlInterpol (.NET library)  
**Started:** 2026-10-02  
**Owner:** sqlinterpol-maintainers  
**Updated:** 2026-10-02

## Success criteria (host)

| Criterion | Result |
|-----------|--------|
| Don't break existing code | Pass — contribution adds docs/scripts/CI wrappers only; no `src/` product edits on the PR branch |
| Token efficiency (LLM sessions) | Improved situating — CANONICAL + Fast paths on **contributor** owners; consumer wiki pages stay package-facing |
| Easier start (human + LLM) | Pass for foundation — intent → CANONICAL → Fast path; product spine = [getting-started.md](../getting-started.md) |
| Instrumentation gaps | Pass for foundation — owners, [ci-flow.md](ci-flow.md), [tribal-inventory.md](tribal-inventory.md), [rank7-dated-queue.md](rank7-dated-queue.md) |
| Consumer vs contributor docs | Pass after correction — wiki allowlist; no CANONICAL/ops/`src/` in wiki topic guides |

## Packages

| # | Package | Status | Evidence |
|---|---------|--------|----------|
| 0 | Install skeleton + wired checks | done | `npm run check:railkit`; architecture roots=`src` (observe-only ratchet) |
| 1 | Specialize Rank 1 for library + LLM use | done | [CANONICAL-SOURCES.md](../CANONICAL-SOURCES.md), UI/auth n/a |
| 2 | Unify ci-flow; tribal; rank7 | done | [ci-flow.md](ci-flow.md), [tribal-inventory.md](tribal-inventory.md), [rank7-dated-queue.md](rank7-dated-queue.md) |
| 3 | Product-doc Fast paths + journal | done | Feature specs skim paths; [railkit-apply-journal.md](railkit-apply-journal.md) |
| 4 | Upstream draft PR (rails only) | done | https://github.com/jimmieulenius/SqlInterpol/pull/4 |

## Reportable moment (kit)

**Claim:** foundation Rank 1 specialize for **LLM/agent situating** on this host; product SQL behavior unchanged on the contribution branch.

## Inventory pointers

- Dual homes addressed: product spine → `getting-started.md`; UI/auth → n/a in CANONICAL + rank7.
- Tribal list: [tribal-inventory.md](tribal-inventory.md).

## Hotspots (observe-only baseline)

Architecture check baselined existing large files; this adopt does **not** refactor them in the PR:

| File | Lines (baseline) |
|------|------------------|
| `SqlAotInterceptorGenerator.Emitter.cs` | 780 |
| `SqlBuilder.cs` | ~512 |
| `SqlTestSuiteGenerator.cs` | 447 |

## Usefulness verdict (2026-10-02)

**Yes — keep Railkit rails** for LLM/human situating on this host (optional engineering layer).

Still clocked: unused public API due 2026-11-15; AOT×cross-dialect UPSERT due 2026-11-15; vendor leftovers due 2026-12-01.
