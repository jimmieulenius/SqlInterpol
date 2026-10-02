# Testing bar

**Purpose:** How to satisfy **governing Rank 2 (verified behavior)** with unit, seam, and e2e proof.  
**Owns:** test expectations when behavior moves.  
**Does not own:** priority ranks ([governing-priorities.md](governing-priorities.md)), host runner choice, chasing coverage percentages.

Tests and CI test jobs are **instruments of Rank 2** (checks/tools that serve that rank; they also support Ranks 3–4). They serve Rank 2; they do not outrank situating (opening the one right owner doc before edit).

## Fast path (read first)

- Yardstick: [governing-priorities.md](governing-priorities.md) — Rank **2** Verified behavior.
- **Behavior move → proof in the same change** (or do not merge).
- **Unit / seam** for domain and API logic; **e2e** when a user-visible path changes and a harness exists.
- **Docs-only / pure rename:** proof not required.
- Missing harness (Rank **7**): dated queue (deferred with owner + due date) + explicit manual or characterization proof (baseline test of current behavior) for this PR — never zero proof on a behavior change.
- CI: [ci-flow.md](ci-flow.md). Adopt: [ADOPTION.md](../ADOPTION.md#correct-sequence).

## Why (consequence)

Rank **2** says execution truth is code **and** a checkable trail. Without tests (or agreed characterization), situating (Rank **1**) still finds the owner, but the edit is unverifiable — agents and humans both ship hope.

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| Same-change unit or seam test for logic you touched | “Test later” on behavior PRs |
| E2e when harness exists for the changed user-visible path | Treating “add e2e job” as more important than Rank **1** owners |
| Fail CI when tests fail — same umbrella as local | Remote-only test jobs editors cannot run |
| Small seam test at the boundary you changed | Huge full-suite run required for a pure rename |
| Queue harness under Rank **7** with owner + date | Claiming Rank **2** satisfied with no proof at all |

## What to add when

| Change type | Minimum proof (Rank 2) |
|-------------|------------------------|
| Domain / store / pure function behavior | Unit or focused seam test |
| API / persistence / access boundary | Seam test at the boundary (+ unit where cheap) |
| User-visible flow | E2e if harness exists; else manual script on PR **and** queue harness (Rank 7) |
| Refactor, no intended behavior change | Existing tests green; characterization if the seam was bare |
| Docs / comments only | None |
| Mechanical rename / re-export only | Existing suite green |

## Host fill-in

| Item | Host value |
|------|------------|
| Unit command | _(e.g. `npm test`)_ |
| E2e command | _(e.g. `npm run test:e2e`)_ |
| When e2e is mandatory vs optional | _(edit; must still honor Rank 2)_ |
| Quarantine / skip policy | _(dated owner; no silent permanent skip — Rank 4)_ |

## Fail if

- Behavior-changing PR merges with no Rank **2** proof.
- E2e harness exists for the surface and a user-visible change skips it without a dated Rank **7** exception.
- Tests deleted or skipped to go green without a removal plan (Rank **4**).

## Related

- Priorities: [governing-priorities.md](governing-priorities.md)
- Layering: [code-quality-and-refactor.md](code-quality-and-refactor.md), skill `layering-review`
- Stability: [ongoing-engineering-bar.md](ongoing-engineering-bar.md)
