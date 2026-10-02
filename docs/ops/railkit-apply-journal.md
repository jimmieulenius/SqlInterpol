# Railkit apply journal — SqlInterpol

**Purpose:** Chronological story and measurement log for applying Railkit to SqlInterpol so **LLM agents and humans** share one situating path. In-repo evidence for the experiment — not chat alone.

**Does not own:** adopt exit checklist ([adopt-receipt.md](adopt-receipt.md)); priority ranks ([governing-priorities.md](governing-priorities.md)).

## Fast path (read first)

- **Status / verdict:** [adopt-receipt.md](adopt-receipt.md)
- **Contribution intent:** adopt governing rails for **LLM usage** (situating, shared checks). **No product SQL behavior change** on the upstream PR branch.
- **How to append:** one dated entry per work block; update [Token ledger](#token-ledger) when a session ends

## Goals (host)

1. Make SqlInterpol easier for LLM agents (and humans) to start and change safely.
2. Determine whether Railkit is useful for that goal.
3. Leave a repeatable story in git — without rewriting library behavior in the adopt PR.

## Story so far (narrative)

On 2026-10-02 this repo received a Railkit kit copy. The question: do shared Fast paths / CANONICAL / tighten-only checks help **LLM sessions** stay on the right owner doc under a practical token budget ([WHY.md](../WHY.md))?

**Act 1 — Install.** Intent router, `AGENTS.md`, docs hubs, `scripts/railkit`, `check:railkit`, architecture observe-ratchet on `src/**/*.cs`.

**Act 2 — Specialize for a library + LLM editors.** CANONICAL without fake UI/auth; `dotnet` + npm in [ci-flow.md](ci-flow.md); tribal + Rank 7 clocks; product-doc Fast paths; this journal.

**Upstream PR:** rails/docs/checks only — https://github.com/jimmieulenius/SqlInterpol/pull/4  

*(Local dogfood briefly tried a `SqlFeatureGate` extract and AppendLine template tests; those are **not** in the upstream PR. Preserved on branch `product/sql-feature-gate` if wanted later as separate product work.)*

## Chronology

| When | Block | What happened | Evidence |
|------|-------|---------------|----------|
| 2026-10-02 | Install + specialize | Kit + library CANONICAL + checks | adopt commit |
| 2026-10-02 | Fast paths + journal | Skim paths on product docs; TODO hygiene | adopt commit |
| 2026-10-02 | Upstream draft PR | Public fork; rails-only RFC | https://github.com/jimmieulenius/SqlInterpol/pull/4 |

### Entry template

```markdown
### YYYY-MM-DD — <short title>

- **Intent / CANONICAL owner opened:** …
- **Change:** …
- **Proof (Rank 2):** … (or docs-only / N/A)
- **Token note:** ledger row <id>
```

## What we measure (value)

| Signal | How we capture it | Why it matters |
|--------|-------------------|----------------|
| Situating cost for LLMs | Owner doc named before edit | Rank **1** — less browse/token tax |
| Shared path | Same commands in [ci-flow.md](ci-flow.md) | Rank **3** |
| Non-loosening | Architecture ratchet did not raise ceilings | Rank **4** |
| Token spend | [Token ledger](#token-ledger) | WHY break-even |
| Product safety | No unintended `src/` behavior change on adopt PR | Host trust |

## Token ledger

| Id | Date | Work block | Method | Total | Notes |
|----|------|------------|--------|-------|-------|
| S0–S3 | 2026-10-02 | Orient + adopt + Fast paths | C / pending A | — | Paste Cursor usage at campaign end |

**Campaign total:** _TBD from Cursor usage UI_

## Closing

- **Tokens spent:** _TBD_
- **What shipped upstream:** Railkit situating rails for LLM/human editors; Fast paths; journal — **not** product refactors
- **Railkit verdict:** yes for situating on this host
- **Kit feedback:** language SDK prerequisite for local `dotnet` proof on non-JS hosts

## Related

- [adopt-receipt.md](adopt-receipt.md)
- [WHY.md](../WHY.md)
- Draft PR: https://github.com/jimmieulenius/SqlInterpol/pull/4
