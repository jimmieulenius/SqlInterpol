# Rank chain example (well-shaped host)

**Purpose:** Walk all seven governing ranks on one coherent story. Composite host with intent router, CANONICAL, thin Cursor adapter, Knip hygiene, and hosting as code.

**Yardstick:** [governing-priorities.md](governing-priorities.md) — [Why this order](governing-priorities.md#why-this-order-dependency-chain).  
**Procedure:** [ADOPTION.md](../ADOPTION.md).

## Fast path (read first)

- Fictional composite host “AcmeDocs” with quality-culture ops (monorepo, dual homes — two docs both deciding the same thing — deploy steps only in chat/wiki).
- Each section: rank → what holds → instrument (check/tool that serves a rank) → fail-if if inverted.
- Teaching sketch only — not a claim about any named external product.

## Host sketch

- JS/TS monorepo (`apps/*`, `packages/*`), existing CI, Cursor in use.
- Dual homes: two “how we release” docs both deciding.
- Large hotspot file in `apps/web` editor pipeline module.
- Deploy steps live in Slack + a wiki page.
- Ideal: Knip already discussed; not on the local=CI umbrella yet.

## Link 1 — Situating

**Hold:** One README Start-here table; CANONICAL task → one owner; Fast paths on deciding homes; `check:documentation*`. Situating means open the one right owner doc before edit.

**Move:** Promote one release owner; demote the other to an index. Fill Core owners (layering, UI, fit, CI).

**Fail if inverted:** Agent opens `docs/` by browse and “adds Knip” while two release docs still both decide.

## Link 2 — Verified behavior

**Hold:** On-contact extract (when you open a file for real work) of the large editor module includes seam/unit proof in the same change ([testing-bar.md](testing-bar.md)).

**Move:** Behavior-changing PR cannot merge on “trust the model.”

**Timing note:** Size ratchet (budget that only gets stricter; link 4/6) may already be baselined; Rank **2** still binds on this touch — size never excuses missing proof.

**Fail if inverted:** Green Knip CI with a behavior PR and no tests.

## Link 3 — Actor-agnostic

**Hold:** Thin `.cursor/` adapter points at `agents/skills/`; same `check:railkit` locally and in CI; session rules load [AGENTS.md stop allowlist](../../AGENTS.md#agent-stop-allowlist). Actor-agnostic means same rules for humans and agents.

**Move:** Agents auto-run must/should; humans decide Prefer/Avoid forks (e.g. admin vs end-user chrome).

**Fail if inverted:** Fat always-on Cursor dump that duplicates Prefer/Avoid; or “agents may skip tests.”

## Link 4 — Non-loosening quality

**Hold:** `architecture-budgets.json` `roots: ["apps","packages"]`; ratchet **baselined** (warn **400** / max **800** defaults); ceilings only tighten.

**Move:** Hotspot over warn → extract **on contact**; do not raise `maxLargestFileLines` to unblock. Not a day-one mass shrink ([Size ratchets](code-quality-and-refactor.md#size-ratchets-breakpoints-and-cost)).

**Fail if inverted:** “Just bump the ceiling for this release” or “agents cannot work until every file is under 400.”

## Link 5 — Code-first how

**Hold:** Tribal inventory lists Slack release + wiki DNS; at least one process becomes a script/contract/skill **or** a dated queue row (deferred with owner + due date).

**Move:** Hosting contract under `infra/contracts/` + assert path (or dated-queue full apply under Rank 7).

**Fail if inverted:** Inventory missing; “we still only know release from #ops.”

## Link 6 — Net-simpler structure

**Hold:** On-contact win deleted pad helpers / split cohesive leaves; dead exports removed when importers vanish.

**Prefer:** ratchet + extract together. **Avoid:** docs/skills about cleanup with no ceiling, or ceilings with cleanup how only in chat.

**Fail if inverted:** Permanent scaffolding “for later clarity” with rising size debt.

## Link 7 — Fit depth (clock)

**Hold:** Named ideal end state. Example clocks:

| Instrument | Clock |
|------------|--------|
| Knip-class | `check:unused-export` → `mode: queued`, owner=platform, due=YYYY-MM-DD — then `mode: knip` on umbrella |
| Full e2e on every PR | Dated row in [rank7-dated-queue.md](../../templates/rank7-dated-queue.md) until harness cost is accepted |
| Vendor apply for cloud DNS | Same template; inventory already exists (link 5) |

**Fail if inverted:** Fake Knip green day one while Rank 1 dual homes remain; or undated “we’ll do Knip later.”

## Reading the chain as adopt waves

| Wave | Links | AcmeDocs proof |
|------|-------|----------------|
| Foundation | 1, early 4/6, 3/5 skeleton | Router + doc checks + size baseline + thin adapter |
| Working loop | 5 inventory, 2+6 on-contact | Tribal list; extract + tests |
| Ideal | 7 clocks | unused-export JSON + template rows |

## Related

- Dogfood install proof (smaller host): [../../examples/first-host/](../../examples/first-host/)
- Seller instrument card: [../../project/digest.md](../../project/digest.md#instrument-card-seller--buyer) (monorepo; not installed into hosts)
