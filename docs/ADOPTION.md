# Adoption playbook

**Purpose:** Move a chaotic host repo toward the Railkit governing framework. Owned by skill [`adopt`](../agents/skills/adopt.md).

**Yardstick:** [governing-priorities.md](ops/governing-priorities.md) (ranks; higher wins). This sequence is the **procedure** that implements those ranks — not a peer checklist of unrelated locks.  
**Code-first:** [CODE-FIRST.md](CODE-FIRST.md). Install: [00-QUICK-START.md](00-QUICK-START.md). Instruments (checks/tools that serve a rank): [code-quality-and-refactor.md](ops/code-quality-and-refactor.md), [testing-bar.md](ops/testing-bar.md).

**Actor-agnostic** (same rules for humans and agents): Rank **3** — same situating path (open the one right owner doc before edit) and merge bar for every editor.

## Fast path (read first)

1. Open [governing-priorities.md](ops/governing-priorities.md); do not invert ranks for convenience. Read [Why this order](ops/governing-priorities.md#why-this-order-dependency-chain).
2. Follow the [correct sequence](#correct-sequence) (implements Ranks 1 → 7).
3. Name the **ideal end state**; **clock** lower instruments under Rank **7** (deferred with owner + due date) — not soft forever-skip ([Customer adjustment](ops/governing-priorities.md#customer-adjustment)).
4. Never raise ratchet ceilings (budgets that only get stricter) to unblock (Rank **4**). Agents auto-run must/should; stop only on [AGENTS.md stop allowlist](../AGENTS.md#agent-stop-allowlist).
5. **Rank 2 on touch:** proof binds when behavior moves (step 6), even though size ratchets land earlier (step 4) as structure support — size does not outrank proof.

## Correct sequence

Order follows ranks: **situating (1)** and early **non-loosening structure (4/6)** before ideal depth (7). Tests on behavior changes are Rank **2** — required when you touch behavior in step 6, even if full e2e CI is still queued.

| Step | Wave | Action | Rank served |
|------|------|--------|-------------|
| 1 | Foundation | Inventory dual homes (two docs both deciding the same thing) + tribal processes | 1, 5 |
| 2 | Foundation | Install skeleton ([00-QUICK-START.md](00-QUICK-START.md)) | 5 |
| 3 | Foundation | Fill [CANONICAL-SOURCES.md](CANONICAL-SOURCES.md); Fast paths; wire `check:documentation*` | **1** |
| 4 | Foundation | Baseline file-size ratchet; wire `check:architecture*` | **4**, **6** |
| 5 | Working loop | Tribal → code inventory (script/contract/skill **or** dated queue) | **5**, **7** |
| 6 | Working loop | On-contact win (when you open a file for real work); **Rank 2 proof if behavior moves** ([testing-bar.md](ops/testing-bar.md)) | **2**, **6** |
| 7 | Ideal (clock OK) | `check:unused-export` (Knip-class or dated queue) on CI umbrella | **4**, **6**, **7** |
| 8 | Ideal (clock OK) | Wire host unit + e2e into CI umbrella if missing | **2**, **4**, **7** |
| 9 | Ideal (clock OK) | UI heuristics + one `ui-review` if UI host; else CANONICAL TODO | **7** |
| 10 | Ideal (clock OK) | Vendor assert/apply, living design page, full tribal automation | **5**, **7** |

### Ideal end state (recommend always)

Named under Rank **7** — ship or **dated queue** (owner + date); do not fake green:

- Rank **1:** intent router + CANONICAL + Fast paths + doc checks
- Rank **2/4:** behavior PRs carry proof; unit (+ e2e when applicable) on the umbrella when harness exists
- Rank **4/6:** architecture ratchet; Knip-class when in scope
- Rank **3/5:** thin adapter; tribal inventory
- UI owners when the host has UI

### Gradual change

Exit when **foundation + working loop** exit checks hold — including Rank **1** situating, Rank **2** proof on behavior you touched, Rank **3** thin adapter (no second policy tree), Rank **4** ceilings that did not rise, Rank **5** tribal inventory (at least one move or dated queue), and Rank **6** on-contact structure win where applicable. Ideal steps 7–10 may be **clocked** (owner + date). Undated / ownerless / eternal soft skip of a named ideal instrument fails Rank **7**. Slipped dates re-enter the working loop. **Never** queue Rank **1** or Rank **2** proof on a behavior-changing seam to “finish Knip first.”

Worked example of all seven links: [rank-chain-example.md](ops/rank-chain-example.md).

## Sequence detail

### 1. Inventory

- List markdown that decides the same concept twice (dual homes).
- Note missing Fast paths on deciding docs.
- List processes that exist only in wiki, portal, or chat (deploy, DNS, CI, release, tokens).

### 2. Install skeleton

Copy hubs, `agents/`, chosen adapter, scripts, example contract, workflow. Keep host product specs; demote duplicates to indexes that point at one owner.

### 3. Document owners (foundation)

For each Core owner row: promote the best existing host doc, or keep the Railkit template and specialize. At most one `.md` path per Open cell. Add `## Fast path` to deciding homes. Wire documentation checks into the host umbrella.

### 4. File-size ratchets (foundation)

Run architecture check against host code roots (`src/**`, or `apps`/`packages` — see [code-quality-and-refactor.md](ops/code-quality-and-refactor.md#architecture-roots-examples)). Set `architecture-ratchet.json` from **current** counts (baseline). Wire into local + CI beside doc checks.

**This step is not a mass shrink.** Defaults: warn **400** / max **800** lines ([Size ratchets, breakpoints, and cost](ops/code-quality-and-refactor.md#size-ratchets-breakpoints-and-cost)). Policy: extract and tighten on contact; do not raise ceilings to pass CI. Real hotspot extraction belongs in [On-contact win](#6-on-contact-win) with Rank **2** proof if behavior moves.

### 5. Tribal → code

For each inventoried process: create script, contract, workflow, or skill, **or** record a dated queue item in the host ops index. Fail adopt only if there is no inventory.

### 6. On-contact win

Pick the active delivery seam. Apply layering or hotspot extraction. **If behavior moves, add or update tests in the same change** ([testing-bar.md](ops/testing-bar.md)): unit/seam for logic; e2e when a user-visible path changes and a harness exists (else manual proof + queue harness). Show the loop works on real work. Keep the adapter thin.

### 7+. Ideal clock

Unused-export / Knip-or-equivalent (`check:unused-export` JSON clock), UI rails, vendor automation — per [Gradual change](#gradual-change). Each deferred item needs owner + date. JSON clock covers unused-export only; other ideals use [rank7-dated-queue.md](../templates/rank7-dated-queue.md). Owner detail for gates: [code-quality-and-refactor.md](ops/code-quality-and-refactor.md#trend-to-better-mechanical).

## Exit checks

**Foundation / working loop (must for exit):**

- [ ] Intent router exists; work names one owner before edit
- [ ] CANONICAL has no dual Open cells for the same task; Fast paths on deciding homes
- [ ] Doc + architecture checks green or intentionally baselined; ceilings only tighten
- [ ] Tribal inventory exists; at least one process moved to code or queued
- [ ] Adapter points at `agents/skills/` without a second policy tree
- [ ] One on-contact win recorded on the active seam
- [ ] Behavior-changing win includes tests per [testing-bar.md](ops/testing-bar.md) (or dated harness queue + explicit manual proof)

**Ideal (ship or dated queue with owner + date):**

- [ ] `check:unused-export` on local+CI (`knip` **or** `queued` with owner + due)
- [ ] UI heuristics enabled on UI hosts (or CANONICAL TODO with owner + date)
- [ ] Remaining tribal items queued with owner + date, or automated

## When status is reportable

Claim progress to the host eng/platform owner only against evidence in-repo (engagement Rank **3** — repo is the artifact):

| Moment | Reportable claim |
|--------|------------------|
| After inventory | Dual homes + tribal list; blockers needing a named decider |
| After install + wired checks | Skeleton landed; local check path named; architecture roots set for host code |
| After foundation exit checks | Rank **1** situating holds; ceilings only tighten |
| After on-contact win | Working loop proven on a real seam (+ Rank **2** proof if behavior moved) |
| Ideal items | Shipped **or** dated queue with owner — never fake green |
| Blocked on human | Stop allowlist hit ([AGENTS.md](../AGENTS.md#agent-stop-allowlist)): heuristic fork, approach conflict, dual home, missing secret |

Do not report “done” from chat notes alone. Prefer an adopt receipt (example: [examples/first-host adopt-receipt](../../examples/first-host/docs/ops/adopt-receipt.md)).

## Finding shape

**must** — invert ranks ([governing-priorities.md](ops/governing-priorities.md)); dual deciding owners; Rank **2** behavior change with no proof; raising ratchet ceilings; CI diverges from local; **undated / ownerless / eternal soft skip** of a named ideal instrument; agent soft-opts out of must/should (“shall I?”, skip nudges).  
**should** — missing Fast path; Rank **7** queue missing owner or date; UI heuristics skipped on a UI host without a dated TODO; CI not wired to local checks.  
**defer** — every Rank **7** instrument green day one; full vendor apply; living design page; perfect doc trim (still clock if named as ideal for this host).

## Improve the kit after a host apply

Feed must/should friction into kit owners (not side notes): Quick Start / install script, this playbook, `scripts/lib/repo-root.js`, adapters, or governing ranks. Ratchets only tighten. Dogfood host in this monorepo: [`examples/first-host/`](../../examples/first-host/).
