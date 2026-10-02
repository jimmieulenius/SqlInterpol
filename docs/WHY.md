# Why this framework

**Owns:** why apply the governing framework to a host codebase — goal, means pointers, durable hedge.  
**Does not own:** commercial salary/tool illustrations or offer model (Railkit monorepo `project/` only); doc-structure ranks ([DOCUMENTATION-PRINCIPLES.md](DOCUMENTATION-PRINCIPLES.md)); size breakpoints ([ops/code-quality-and-refactor.md](ops/code-quality-and-refactor.md#size-ratchets-breakpoints-and-cost)).

**Meta** — Keep this short. Point at how owners; do not restate their tables.

## Fast path (read first)

1. **Goal:** every editor (human or agent) stays productive inside a practical tool-cost and attention budget. Clear the host’s **break-even bar** (`tool cost / labor cost`) instead of burning the subscription on **situating** (not opening the one right owner doc) and rework tax.
2. **Means:** project truth reachable as text in the repo; seams small enough for cheaper models; one deciding owner per concept — see [Means](#means).
3. **Hedge:** the same shape helps humans and the organization if agents are declined or models change (**actor-agnostic** = same rules for humans and agents).
4. **Fail if:** rules are followed with no link to this goal, or size ceilings are raised “to unblock” agents.

## Goal

LLM-assisted editing is cheap relative to a developer’s loaded cost when sessions stay short and on the right owner. Without shared situating and size discipline, token spend and rework erase that advantage. This framework exists so the host clears (and keeps clearing) that bar toward coherence, efficiency, clarity, and quality.

Commercial illustration numbers (salary, subscription class, golden-application observations) live only in the Railkit monorepo `project/` surface and are **not** installed into hosts. Mechanism here is host-safe.

## Means

| Need | Owner (do not copy) |
|------|---------------------|
| Repeatable how in `scripts/`, CI, contracts, skills | [CODE-FIRST.md](CODE-FIRST.md) |
| One deciding owner; indexes only point; plain language | [DOCUMENTATION-PRINCIPLES.md](DOCUMENTATION-PRINCIPLES.md) |
| File-size ratchets (warn **400** / max **800** defaults); cost reasoning | [ops/code-quality-and-refactor.md — Size ratchets](ops/code-quality-and-refactor.md#size-ratchets-breakpoints-and-cost) |
| Priority when checks conflict | [ops/governing-priorities.md](ops/governing-priorities.md) |

Everything that relates to the project should exist as text (or code) in the repo so every editor can load it. Prefer seam-local files over always-on dumps.

## Hedge (wins without agents)

Agents work in natural language. Making the repo reachable, sized, and normalized for them also makes it clearer for people and cheaper for the organization to onboard, review, and change course. If the host opts out of cloud agents, Ranks **1**–**4** still pay: right owner, verified behavior, shared path, non-loosening structure.

## Fail if

- Editors treat owners, Fast paths, or size ratchets as bureaucracy with no link to cost and quality.
- Ceilings rise to “unblock” a model or a deadline ([governing-priorities.md](ops/governing-priorities.md) Rank **4**).
- A second “for agents only” rulebook appears beside this path (Rank **3**).
