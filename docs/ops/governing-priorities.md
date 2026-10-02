# Governing priorities

**Purpose:** Ranked yardstick for the Railkit governing framework and change projects. Higher rank wins when checks or tools conflict.  
**Owns:** priority ranks, how customer needs adjust depth, and which practices are **consequences** (not peer goals).  
**Does not own:** how to run a specific gate ([testing-bar.md](testing-bar.md), [code-quality-and-refactor.md](code-quality-and-refactor.md), [ci-flow.md](ci-flow.md)), adopt step list ([ADOPTION.md](../ADOPTION.md)), doc-structure ranks ([DOCUMENTATION-PRINCIPLES.md](../DOCUMENTATION-PRINCIPLES.md)), agent stop allowlist detail ([AGENTS.md](../../AGENTS.md#agent-stop-allowlist)).

## Fast path (read first)

- Conflicts: [Ranked priorities](#ranked-priorities) — higher wins.
- Tests, CI, Knip, and size ratchets (budgets that only get stricter) are **instruments** — checks or tools that serve these ranks — see [Consequences](#consequences-not-peer-goals).
- Rank **7** = **clocked** depth-fit (deferred with owner + due date), not soft forever-skip.
- Agents auto-run must/should; stop only per [AGENTS.md stop allowlist](../../AGENTS.md#agent-stop-allowlist).
- Adopt order implements this yardstick: [ADOPTION.md](../ADOPTION.md#correct-sequence).
- Worked seven-link example: [rank-chain-example.md](rank-chain-example.md).

## Why

A pile of “must wire X” checklists drifts into cargo-cult gates. Soft optional nudges drift into skipped work. Railkit’s yardstick is ranks + fail-if, then procedures as consequences. Customers differ in harness maturity; they get **dated queues** (deferred with owner + due date), not silent skips of situating (opening the one right owner doc before edit) or unverified behavior change.

### Why this order (dependency chain)

Higher ranks are prerequisites, not peer preferences:

1. **Situating** (open the one right owner doc before edit) — Without one deciding owner, every later gate and agent session thrash on the wrong docs.
2. **Verified behavior** — Once you know where to change, the change needs a checkable trail; otherwise situating aims hope.
3. **Actor-agnostic** (same rules for humans and agents) — Humans and agents share the same path and merge bar; otherwise Rank 1–2 become optional for one editor class. Agent auto-loop lives here.
4. **Non-loosening quality** — Under date pressure, entropy wins unless budgets only tighten (and Rank 1–3 already bind).
5. **Code-first how** — Repeatable process left in wiki/chat will be skipped; scripts/skills keep Rank 3 true over time.
6. **Net-simpler structure** — Size/dead-code pressure only helps after owners and proof exist; otherwise you rearrange noise. Prefer: ratchet + extract. Avoid: skill/docs without a ceiling, or ceilings without code-first how for the recurring cleanup. **Foundation baselines** size; **on contact** (when you open a file for real work) shrinks hotspots — not a day-one mass shrink, and not “agents cannot edit large files” ([code-quality-and-refactor.md](code-quality-and-refactor.md#size-ratchets-breakpoints-and-cost)).
7. **Fit depth** — Ideal instruments (Knip-class, full e2e on CI, vendor apply) are **named and clocked**, never faked green and never soft-skipped; Rank 7 never excuses skipping 1–2.

**Adopt timing vs rank numbers:** Rank **2** binds on the **first behavior-changing touch** (working-loop on contact). An early file-size ratchet (Ranks **4/6**) is foundation *structure* support — it does **not** outrank Rank **2** proof when behavior moves. Sequence: [ADOPTION.md](../ADOPTION.md#correct-sequence).

**Not the same yardstick:** doc-structure ranks ([DOCUMENTATION-PRINCIPLES.md](../DOCUMENTATION-PRINCIPLES.md)) and engagement ranks (`project/consulting/engagement-priorities.md`, not installed) are separate. This file owns **host governing** ranks only.

## Ranked priorities

| Rank | Principle | Means in one line | Fail if… |
|------|-----------|-------------------|----------|
| **1** | Situating | One intent path; one deciding owner per task before edit | Dual homes (two docs both deciding the same thing); folder-browse as the plan; agent/human without a named owner |
| **2** | Verified behavior | Execution truth is code **and** proof; behavior moves leave a checkable trail | Behavior PR with no test/characterization proof; “trust the model” |
| **3** | Actor-agnostic | Same situating path and merge bar for every editor | Parallel “for agents” vs “for humans” rulebooks; agent soft-opts out of must/should |
| **4** | Non-loosening quality | Budgets and bars only tighten; entropy does not win under date pressure | Raising ceilings to unblock; silent skip of failing gates |
| **5** | Code-first how | Repeatable how lives in scripts, contracts, workflows, skills | Wiki/chat as the only how for a recurring process |
| **6** | Net-simpler structure | Coherence and clarity in the tree; delete or extract rather than wrap forever | Permanent scaffolding; size/dead-code ceilings that only rise |
| **7** | Fit depth to the host | Name the ideal end state; **clock** lower instruments (owner + date) | Fake every ideal gate green day one; skip Rank 1–2 to “move fast”; **undated / ownerless / eternal soft skip** of a named ideal instrument |

## Consequences (not peer goals)

| Instrument | Serves rank(s) | Notes |
|------------|----------------|-------|
| Intent router, CANONICAL, Fast paths, `check:documentation*` | **1** | Foundation of adopt — not optional “docs nicety” |
| Unit / seam / e2e / characterization ([testing-bar.md](testing-bar.md)) | **2** (also **3**, **4**) | Required because behavior must be verified — not because “we like CI” |
| Local = CI umbrella ([ci-flow.md](ci-flow.md)) | **3**, **4** | Gates bind every editor the same way |
| File-size / hotspot ratchet | **4**, **6** | Early mechanical support; **baseline then on-contact extract** (warn 400 / max 800 defaults) — cost/entropy, not “agents cannot work” |
| Unused-export / Knip-class | **4**, **6** | Ideal depth; **clockable** under Rank **7** — kit ships `check:unused-export` (`queued` \| `knip`) |
| Thin adapters / skills | **3**, **5** | Policy stays in docs; tools stay thin |
| Tribal → code inventory | **5**, **7** | Inventory day one; full automation may be clocked |
| Agent auto-loop | **3**, **5** | When an agent is the editor: run skills and same-turn must/should without permission prompts; pause only on [stop allowlist](../../AGENTS.md#agent-stop-allowlist) |

**Do not** promote an instrument above the rank it serves. Example: wiring Knip while dual owners remain is Rank **7** theater that violates Rank **1**.

**Clocked queue ≠ skip:** a Rank **7** queue without owner and date is a soft skip (fail Rank **7**). Slipped dates re-enter the working loop.

**Where clocks live:** unused-export / Knip-class uses mechanical `unused-export.json` (`owner` + `due`) via `check:unused-export`. Other ideal instruments (UI rails, vendor apply, full e2e suite) use [rank7-dated-queue.md](../../templates/rank7-dated-queue.md) (or the host ops copy) until a dedicated check exists.

## Customer adjustment

Use ranks to negotiate depth — not to invert them.

| Host need | May clock / thin (Rank 7) | Must not drop |
|-----------|---------------------------|---------------|
| No e2e harness yet | Full e2e suite in CI (owner + date) | Rank **2** proof on behavior changes (unit/seam or characterization + dated harness queue) |
| Strict privacy / no cloud agents | Fancy agent setups | Rank **1**–**4** with human-led editing + local/CI gates |
| Legacy size debt | Aggressive extract sweep | Rank **4** (baseline ceilings; never raise to unblock) |
| Many packages | Org-wide Knip day one | Rank **1** router for the packages in scope first |
| Date pressure | Ideal adopt steps 7–10 with clocks | Rank **1** situating and Rank **2** proof on the seam you touch |
| Agent is the editor | — | Soft opt-out of must/should (“shall I?”, skip nudges); stop only on allowlist |

When two instruments conflict, name the deciding **rank** (same habit as UI principles).

## Related

- Adopt sequence: [ADOPTION.md](../ADOPTION.md)
- Agent stop allowlist: [AGENTS.md](../../AGENTS.md#agent-stop-allowlist)
- Skill: [priorities-review.md](../../agents/skills/priorities-review.md)
- Chain example: [rank-chain-example.md](rank-chain-example.md)
- Testing instrument: [testing-bar.md](testing-bar.md)
- Structure / trend instruments: [code-quality-and-refactor.md](code-quality-and-refactor.md)
- Doc-structure ranks (separate yardstick): [DOCUMENTATION-PRINCIPLES.md](../DOCUMENTATION-PRINCIPLES.md)
