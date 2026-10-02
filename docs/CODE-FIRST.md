# Code-first

**Purpose:** Global Prefer/Avoid for repeatable work. Ops, CI, and DNS are examples, not the whole rule.

**Classification:** Meta  
**Related:** [DOCUMENTATION-PRINCIPLES.md](DOCUMENTATION-PRINCIPLES.md) (why vs how), [ops/integrations-as-code.md](ops/integrations-as-code.md), [ops/ci-flow.md](ops/ci-flow.md), [`scripts/`](../scripts/README.md), [`infra/`](../infra/README.md), [`agents/skills/`](../agents/README.md).

## Fast path (read first)

- **Rule:** if a human or agent must do it twice, put it in the repo as executable or declarative code.
- **Prose role:** why, contracts, Prefer/Avoid. Link to the path that runs.
- **Judgment stays prose:** UI ranks, fit ranks, layering Prefer/Avoid are yardsticks, not JSON linters.
- **Adopt:** inventory tribal steps; **dated-queue** (deferred with owner + due date) script/contract/skill moves. Do not fail adopt for unfinished history if the inventory exists ([ADOPTION.md](ADOPTION.md)).

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| `scripts/`, `infra/contracts/`, workflows, and skills own how | Wiki / portal / chat as the only how |
| Same npm script locally and in CI | Remote-only check logic |
| Thin docs that link to scripts | Second Markdown how-tree beside code |
| Skills as agent-facing procedures | Pasting 400-line policy into always-on editor context |
| Contract JSON + assert/apply script for vendors | "Click the console" as institutional memory |
| Inventory + dated-queued moves during adopt | Pretending the host is fully automated on day one; undated eternal soft skip |

## Fail if

- A new process, gate, or "how we always do X" is added only as wiki, PR comment, or Markdown how beside `scripts/` / `infra/` / workflows / skills.
- CI diverges from local check entrypoints without a documented, temporary exception.

## Domain map (specializations)

| Domain | Code-first shape |
|--------|------------------|
| Quality / entropy | Instruments (checks/tools that serve a rank) of [governing-priorities.md](ops/governing-priorities.md) Ranks **4**/**6**; order in [ADOPTION.md](ADOPTION.md#correct-sequence) |
| Tests | Instrument of Rank **2** — [testing-bar.md](ops/testing-bar.md) |
| CI | Same umbrella as local (Ranks **3**/**4**); Rank **7** depth **clocked** (owner + date) |
| Integrations / hosting / DNS | [`infra/contracts/`](../infra/contracts/) + apply/assert scripts |
| Docs structure | Ownership / link / budget checkers |
| Design tokens | In-repo tokens/components; living yardstick page Rank **7** depth |
| Agent behavior | [`agents/skills/`](../agents/skills/) + thin adapters; auto must/should; [stop allowlist](../AGENTS.md#agent-stop-allowlist) |
