# Skill: priorities-review

**Owner:** [governing-priorities.md](../../docs/ops/governing-priorities.md)  
**Edit-time:** when instruments (checks, tests, or tools that serve a rank — e.g. CI, Knip, size ratchets, adopt depth) conflict or feel cargo-cult (kept because they exist, not because a rank needs them)

Default bias: name the deciding rank; demote instruments that jumped above the rank they serve. Owner-doc rank edits only after repeated drift (confirm).

## Efficient pass

One subject sentence (`priority:` / `instrument:`). One review skill per turn. Open governing-priorities Fast path first. Same-turn must/should fixes.

## Loop

1. Name the conflict (e.g. Knip before owners; e2e theater; raising ceilings).
2. Map each side to a [rank](../../docs/ops/governing-priorities.md#ranked-priorities) or [instrument](../../docs/ops/governing-priorities.md#consequences-not-peer-goals).
3. Higher rank wins; rewrite the plan so instruments follow.
4. Customer depth: use [Customer adjustment](../../docs/ops/governing-priorities.md#customer-adjustment) — **clock** Rank **7** (owner + date) only; never invert **1**–**2**; undated eternal soft skip fails Rank **7**.
5. Agent editor: auto must/should; stop only on [AGENTS.md stop allowlist](../../AGENTS.md#agent-stop-allowlist).
6. Point adopt/CI/testing owners; do not paste Prefer/Avoid into the skill. Teaching story: [rank-chain-example.md](../../docs/ops/rank-chain-example.md).

## Finding shape

**rank · conflict · fail-if · fix · severity**  
**must** = Rank 1–2 inverted; instrument treated as peer goal above situating (open the one right owner before edit) / verified behavior; ceilings raised to unblock; undated Rank 7 soft skip; agent “shall I?” skip of must/should.  
**should** = Rank 7 queue missing owner or date; stale adopt order vs ranks.  
**defer** = pure trim of checklist wording.
