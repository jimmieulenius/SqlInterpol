# Code quality and refactor

**Purpose:** Dedupe, modularize, net simpler, where-things-live, size-debt ratchets (budgets that only get stricter). Canonical principles for structure; stack details are host fill-ins.

## Fast path (read first)

- **Dedupe:** one owner module for non-trivial logic/copy/config; reference, do not copy-paste.
- **Net simpler:** delete or inline unused scaffolding before wrapping; future shape needs a named owner destination.
- **First-pass splits:** `parent/<feature>/` + `index.ts` (or language equivalent) in the same change; index re-exports only the external surface.
- **Ratchets:** extract and tighten; never raise ceilings to unblock ([architecture-ratchet.json](../../scripts/railkit/architecture/architecture-ratchet.json)) — instrument (check/tool that serves a rank) of [governing-priorities.md](governing-priorities.md) Ranks **4** and **6**.
- **Breakpoints (defaults):** warn **400** lines; max **800** lines — see [Size ratchets and cost](#size-ratchets-breakpoints-and-cost).
- **First pass ≠ mass shrink:** foundation **baselines** the ratchet; shrink **on contact** (when you open a file for real work), not a day-one sweep.
- **Trend gates:** instruments of those ranks; sequence and customer depth: [governing-priorities.md](governing-priorities.md), [ADOPTION.md](../ADOPTION.md#correct-sequence).
- **Layering:** [application-layering.md](../engineering/architecture/application-layering.md).

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| Current call sites or a real boundary earn the shape | Machinery justified only by "more explicit" or unnamed future |
| Cohesive extraction over split-for-line-count | Relocation without removing duplication |
| Leaf imports for heavy ops | Barrel index files that pull heavy write/pipeline modules into every light import |
| One obvious entry per concern; remove superseded exports in the same change | Long-lived deprecated aliases for internal modules |
| Rank **1** owners before Knip-class depth (Rank **7**) | Skipping situating (opening the one right owner doc before edit) to bolt on dead-code tooling |
| Baseline size ratchet, then extract on contact | Day-one sweep of every fat file; raising warn/max ceilings to “unblock” |
| Seam-local edits under warnLines when possible | Treating size shrink as a hard “agents cannot edit” gate (it is cost and mess control, not a capability limit) |

## Trend to better (mechanical)

Instruments of [governing-priorities.md](governing-priorities.md) Ranks **4** and **6** (non-loosening quality, net-simpler structure). Actor-agnostic (same rules for humans and agents; Rank **3**).

**Ideal end state:** local + CI umbrella includes doc ownership (Rank **1**), file-size ratchet (Ranks **4/6**), unused-export / Knip-class (Ranks **4/6**, **clockable** under Rank **7** — deferred with owner + due date). Ceilings only tighten.

**Order** (see [ADOPTION.md](../ADOPTION.md#correct-sequence)):

1. Document owners + `check:documentation*` — Rank **1**
2. File-size ratchet + `check:architecture*` — Ranks **4**, **6**
3. Unused-export / Knip-or-equivalent — Rank **7** may be **dated-queued** after 1–2 hold (owner + date; not soft forever-skip)

| Gate class | Kit ships | Host (change project) |
|------------|-----------|------------------------|
| Doc ownership / budgets | `check:documentation*` | Wire for Rank **1** |
| File-size / hotspot ratchet | `check:architecture*` | Baseline ceilings (Ranks **4**, **6**); set `roots` to host packages |
| Unused exports / dead modules | `check:unused-export` + [`unused-export.json`](../../scripts/railkit/unused-export/unused-export.json) | Mode `knip` or `queued` (owner + due); language-equivalent tool per matrix below |

**Fail if:** claiming Rank **4/6** while Rank **1** dual homes (two docs both deciding the same thing) remain. **Should:** Knip-class still queued with owner + date after foundation exit. **Must:** undated / ownerless eternal soft skip of Knip-class when it is a named ideal for the host.

### Language-equivalent (Knip-class)

| Ecosystem | Example tool | Notes |
|-----------|--------------|-------|
| JS / TS | [Knip](https://github.com/webpro-nl/knip) | Default `mode: knip` via `npx knip` |
| Python | vulture / deadcode-class analyzers | Wire host command into `unused-export.json` `command`/`args` or keep `queued` |
| Go | `staticcheck` unused / `deadcode` | Same — host command or dated queue |
| .NET | IDE analyzers / custom unused public API gates | Same |
| Other | Host-chosen unused-export detector | Must be checkable locally + CI; else Rank **7** clock |

Template for non-unused Rank **7** rows: [rank7-dated-queue.md](rank7-dated-queue.md).

### Architecture `roots` examples

In `architecture-budgets.json` (next to the architecture check scripts):

| Host shape | Example `roots` |
|------------|-----------------|
| Single package | `["src"]` |
| Apps + packages monorepo | `["apps", "packages"]` |
| Kit self-check | `["scripts", "agents", "adapters"]` |

## Where things live (SqlInterpol)

| Thing | Location |
|-------|----------|
| Public builder API | `src/SqlInterpol/SqlBuilder*.cs`, `SqlBuilderExtensions.*` |
| Pipeline / rewriters / feature gates | `src/SqlInterpol/Pipeline/` |
| Dialects | `src/SqlInterpol/Dialects/` |
| Schema metadata | `src/SqlInterpol/Schema/` |
| Integrations | `src/SqlInterpol.Dapper`, `.EntityFrameworkCore`, `AdoNet/` |
| Generators / analyzers | `src/SqlInterpol.Generators`, `.Analyzers` |
| Layering Prefer/Avoid | [application-layering.md](../engineering/architecture/application-layering.md) |

## Size ratchets, breakpoints, and cost

Owns the **reasoning** for file-size instruments (Ranks **4**/**6**). Cost/break-even framing for hosts: [WHY.md](../WHY.md). Adopt timing: [ADOPTION.md](../ADOPTION.md#4-file-size-ratchets-foundation).

### What foundation does (and does not)

| Phase | Size work | Why |
|-------|-----------|-----|
| Foundation (adopt step 4) | Run `check:architecture*`; **baseline** `architecture-ratchet.json` from current counts; wire local+CI | Stops silent growth; does **not** require mass shrink day one |
| Working loop (on-contact) | When a hotspot is opened, extract toward warn; tighten ratchet ceilings | Real shrink happens on delivery seams, with Rank **2** proof if behavior moves |
| Ongoing | Never raise ceilings to unblock; improve on contact; no sweeps | Rank **4** non-loosening |

**Fail if:** “First pass = delete/split everything over warn before agents touch the repo.” That inverts Rank **1** (owners first) and invents a myth that agents cannot edit large files.

### Breakpoints (kit defaults)

Configured in [`architecture-budgets.json`](../../scripts/railkit/architecture/architecture-budgets.json) (hosts may specialize; ceilings only tighten afterward):

| Knob | Default | Effect |
|------|---------|--------|
| `warnLines` | **400** | Soft hotspot: WARN in check; **touch rule** = prefer one cohesive extraction in the same change |
| `maxLines` | **800** | Hard fail if any scanned file exceeds |
| Ratchet `maxFilesOverWarn` | Baselined from host | Count of files over warn may only fall |
| Ratchet `maxLargestFileLines` | Baselined (≤ maxLines) | Largest file may only shrink |

### Why these numbers (hardened reasoning)

- **400 warn** — above this, a single editor/agent turn tends to load a blob that mixes concerns; extract restores a seam-local unit of work (cheaper situating + fewer accidental edits).
- **800 max** — hard stop on unbounded growth; past this, reviews and Rank **2** proof degrade regardless of model.
- **Not a claim:** “Agents cannot work above 400 lines.” Agents *can* edit large files; cost, error rate, and rework rise. The ratchet is **mess + token insurance**, actor-agnostic (same rules for humans and agents; Rank **3**).

### Token / cost link

| Driver | Effect on spend |
|--------|-----------------|
| Dual homes / no Fast path (Rank **1**) | Highest early tax — wrong docs loaded every session |
| Fat hotspot **without** a ratchet | Repeated full-file loads + thrash; density beats raw LOC |
| Fat hotspot **with** baseline + on-contact extract | One-time extract cost; later sessions stay seam-local |
| Raising ceilings to unblock | Permanent tax increase; violates Rank **4** |

**Order of leverage:** fix situating first, then baseline size, then shrink on contact. Size alone does not fix wrong-owner sessions.

## Large files (touch rule)

When editing a file over `warnLines`, prefer one cohesive extraction in the same change. Hotspot queue: improve on contact; do not sweep. One writer per hotspot file on a shared checkout.

## Cleanup batches

When the task is cleanup: one cluster (~8–30 files); stop if the next step needs behavior change or import cycles.
