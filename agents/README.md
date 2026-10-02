# Agent skills (editor-agnostic)

Canonical skill bodies live here as plain markdown. Editor tools get **thin adapters** that point here ([adapters/README.md](../adapters/README.md)).

| Skill | Owner doc | When |
|-------|-----------|------|
| [adopt.md](skills/adopt.md) | [ADOPTION.md](../docs/ADOPTION.md) | Bootstrap host into Railkit |
| [priorities-review.md](skills/priorities-review.md) | [governing-priorities.md](../docs/ops/governing-priorities.md) | Check vs rank conflicts on a host |
| [engagement-review.md](skills/engagement-review.md) | [engagement-priorities.md](../../project/consulting/engagement-priorities.md) | Offer / qualify / pitch surface / privacy / commercial hubs (monorepo) |
| [doc-review.md](skills/doc-review.md) | [DOCUMENTATION-PRINCIPLES.md](../docs/DOCUMENTATION-PRINCIPLES.md) | Ownership / dual homes / caches / plain language / hub routing |
| [layering-review.md](skills/layering-review.md) | layering + code-quality | Placement / simpler after change / size debt |
| [stability-review.md](skills/stability-review.md) | [ongoing-engineering-bar.md](../docs/ops/ongoing-engineering-bar.md) | Side writes / leftover forks |
| [ui-review.md](skills/ui-review.md) | [ui-principles.md](../docs/product/heuristics/ui-principles.md) | Design / chrome pass |
| [fit-review.md](skills/fit-review.md) | customer-targets + customer-fit | Should we build / claim this |
| [ci-gate.md](skills/ci-gate.md) | [ci-flow.md](../docs/ops/ci-flow.md) | What to run / merge |
| [commit.md](skills/commit.md) | Host commit policy / ci-flow | User asked to commit |
| [shared-checkout.md](skills/shared-checkout.md) | ci-flow shared checkout | Exclusive commands / mixed hunks |
| [release.md](skills/release.md) | ci-flow release section | Ship / tag / deploy |

**Contract:** Prefer/Avoid and ranks stay in owner docs. Skill bodies are decision tables and loops. Do not grow a second policy tree inside an editor folder. Write plainly ([DOCUMENTATION-PRINCIPLES.md#plain-language](../docs/DOCUMENTATION-PRINCIPLES.md#plain-language)).
