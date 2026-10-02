# Skill: engagement-review

**Owner:** [engagement-priorities.md](../../../project/consulting/engagement-priorities.md)  
**Edit-time:** when offer, qualification, agent setup, privacy, claims, pitch, or commercial hubs (`README.md`, `project/README.md`, `project/marketing/*`, `project/consulting/*`) are edited or reviewed

Default bias: name the deciding engagement rank; demote instruments (checks or practices that serve a rank) that jumped the queue; fix pitch-surface Prefer/Avoid same turn **when the Prefer/Avoid still fits**. Confirm before editing the rank table or Pitch surface Prefer/Avoid.

Path note: skill body lives under `kit/agents/skills/`; owner doc lives under monorepo `project/consulting/` (not installed into hosts).

## Efficient pass

One subject sentence (`engagement:` / `claim:` / `setup:` / `pitch:` / `pushback:`). One review skill per turn. Open engagement-priorities Fast path first (ranks + [Pitch surface Prefer / Avoid](../../../project/consulting/engagement-priorities.md#pitch-surface-prefer--avoid)). Same-turn must/should fixes **or** a numbered yardstick fork.

## Loop

1. Name the tension (e.g. close deal vs no decider; Cursor upsell vs Copilot org; privacy vs cloud agents; hub unclear on sell/buyer/bring; Prefer/Avoid blocks a coherent job).
2. Map to [engagement ranks](../../../project/consulting/engagement-priorities.md#ranked-priorities).
3. Apply [Pitch surface Prefer / Avoid](../../../project/consulting/engagement-priorities.md#pitch-surface-prefer--avoid) when the corpus is commercial hubs or offer/positioning/why/proposal/qualification.
4. **Pushback check:** if the yardstick is wrong for the job (not merely inconvenient), stop with a numbered fork — (1) keep Prefer/Avoid / fail-if and fix docs, or (2) edit the owner Prefer/Avoid / Rank fail-if (confirm) then align docs. Shared rule: [DOCUMENTATION-PRINCIPLES.md — Self-improving loop](../../docs/DOCUMENTATION-PRINCIPLES.md#self-improving-loop).
5. Higher wins; point the instrument owner (qualification, agent setups, privacy, offer, USP, positioning).
6. If the conflict is **inside the host repo** (owners, tests, CI), switch to skill `priorities-review` — do not resolve host Rank 1–2 here.
7. Do not paste Prefer/Avoid into this skill body.

## Finding shape

**rank · conflict · fail-if · fix · severity**  
**must** = Rank 1–2 inverted (bad fit or no decision rights); claims that violate do-not-claim; rules-dump or AI-seat sale sold as Railkit; monorepo/`project/` README with no route to sell / buyer / customer-brings / you–we–exit; Prefer/Avoid silently ignored with no fork.  
**should** = agent setup optimized for speed over framework fit; privacy story conflates kit-in-git with vendor egress; missing 60s pitch or primary-buyer default; You/we or Customer brings only implied; Prefer/Avoid fight stated only as soft “options exist.”  
**defer** = trim duplicate pitch wording; invent fixed catalog price or week counts.  
**pushback** = numbered fork to change Pitch surface Prefer/Avoid or Rank fail-if (confirm); not a soft skip of the review.
