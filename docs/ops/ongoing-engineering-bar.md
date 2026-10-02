# Ongoing engineering bar

**Purpose:** Standing rules for day-to-day code touches. Not a project tracker. Finished work lives in code, tests, CI, and owner docs.

## Fast path (read first)

- **Hotspots:** when product work opens a large queued file, extract one clear leaf; do not sweep the tree.
- **Shared contracts:** if path A works and path B fails for the same rule, route both through one source of truth (SSOT). Do not add a third priority chain.
- **Side effects:** one domain coordinator owns write order and cross-module side effects; leaf modules only signal intent.
- **Code-first:** [CODE-FIRST.md](../CODE-FIRST.md). **Structure detail:** [code-quality-and-refactor.md](code-quality-and-refactor.md).
- **Tests:** Rank **2** ([testing-bar.md](testing-bar.md)); priorities: [governing-priorities.md](governing-priorities.md).

## Shared contracts vs leftover forks

**Symptom:** one surface is correct in production; a sibling surface is wrong.  
**Cause:** a shared contract exists, but an old path still uses the old graph.  
**Touch rule:** name the SSOT; move the failing surface onto it; keep delivery variants as efficiency choices, not a second priority chain.

## Side effects and stable identity

| Prefer | Avoid |
|--------|--------|
| Coordinator owns write order and invalidation | Parallel leaf entry points that each write |
| Explicit clocks / versions for stale apply | Silent overwrite of newer state |
| Idempotent repairs behind rare routes | Taxing every hot request with full verifies |

## Release and export hygiene (index)

Unused exports, size budgets, pre-release static gates: host CI + [ci-flow.md](ci-flow.md). Skill: `stability-review`.
