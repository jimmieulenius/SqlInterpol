# Agent session rules (template)

Copy into the host as `.cursorrules`, Claude project instructions, or equivalent. Adjust paths if Railkit files live under a subfolder. Full intent table: README.md (Start here by intent). `llm.txt` is a short mirror only.

## Session start

Pick one intent from README.md (Start here by intent) or llm.txt. Do not assume code work.

Before first edit: name the task in one sentence; open only that row's owner Fast path or named section. Re-open the right doc when blocked or the work shifts.

## Compressed intent list

- install / adopt -> docs/00-QUICK-START.md ; docs/ADOPTION.md ; skill adopt
- implement / fix / refactor -> AGENTS.md -> docs/CANONICAL-SOURCES.md (one row)
- code-first / tribal to code -> docs/CODE-FIRST.md
- CI / merge -> docs/ops/ci-flow.md ; skill ci-gate
- layering -> docs/engineering/architecture/application-layering.md + docs/ops/code-quality-and-refactor.md ; skill layering-review
- priorities -> docs/ops/governing-priorities.md ; skill priorities-review
- stability -> docs/ops/ongoing-engineering-bar.md ; skill stability-review
- tests -> docs/ops/testing-bar.md
- UI review -> docs/product/heuristics/ui-principles.md ; skill ui-review
- fit -> docs/product/heuristics/customer-targets.md then customer-fit.md ; skill fit-review
- doc review -> docs/DOCUMENTATION-PRINCIPLES.md ; skill doc-review

## Standing constraints

- Policy in docs/ and agents/skills/; adapters stay thin (adapters/README.md).
- Size budgets only get stricter; never raise ceilings to unblock.
- Repeatable how = scripts, infra, workflows, skills (docs/CODE-FIRST.md).
- Plain text in kit docs and scripts (no emoji). Write plainly (docs/DOCUMENTATION-PRINCIPLES.md#plain-language).
- Agents: do must/should work without asking; stop only on AGENTS.md stop allowlist (Prefer/Avoid forks, approach conflicts, dual homes, missing secrets). No “shall I?”. Rank 7 = dated queue + continue, not a soft skip.
