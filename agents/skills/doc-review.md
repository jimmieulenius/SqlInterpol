# Skill: doc-review

**Owner:** [DOCUMENTATION-PRINCIPLES.md](../../docs/DOCUMENTATION-PRINCIPLES.md)  
**Edit-time:** development-docs lifecycle via adapters; also when editing monorepo `README.md`, `project/**`, or `kit/docs/**`

Default bias: adjust documentation toward the yardstick (structure **and** [plain language](../../docs/DOCUMENTATION-PRINCIPLES.md#plain-language)) **when Prefer/Avoid still fits**. Owner-doc / Prefer/Avoid edits are the pushback path (confirm) — see [Self-improving loop](../../docs/DOCUMENTATION-PRINCIPLES.md#self-improving-loop).

## Efficient pass

One subject sentence (`ownership:` / `structure:` / `prose:` / `hub:` / `pushback:`). One review skill per turn. Smoke `npm run check:documentation:ownership` before dumping essays. Same-turn must/should fixes **or** a numbered yardstick fork.

## Loop

1. Name the corpus.
2. Rank 1–2: one deciding owner; indexes only point.
3. Rank 3–4: one job per section; layer honesty (decide ≠ index ≠ claim ≠ journey ≠ **cache**). Prefer owners over aggregates ([DOCUMENTATION-PRINCIPLES.md](../../docs/DOCUMENTATION-PRINCIPLES.md#documentation-structure)).
4. Why vs how / code-first: delete duplicate how; leave pointers.
5. Plain language: shorten; replace unexplained jargon; define kit terms on first use or link the owner Fast path.
6. **Hubs:** if corpus is monorepo `README.md` or `project/README.md`, they must **route** named jobs without deciding. For commercial pitch jobs (sell / buyer / bring / exit), also run or hand off to skill `engagement-review` ([Pitch surface Prefer / Avoid](../../../project/consulting/engagement-priorities.md#pitch-surface-prefer--avoid)) — do not invent a second pitch policy here.
7. **Pushback check:** if a structure Prefer/Avoid or fail-if is wrong for a real doc job, numbered fork — (1) keep yardstick and fix docs, or (2) edit [DOCUMENTATION-PRINCIPLES.md](../../docs/DOCUMENTATION-PRINCIPLES.md) (confirm) then align docs. Do not silently ignore ranks.
8. Extend gates when the same fail repeats twice (with the yardstick still believed).

## Finding shape

**rank · file · fail-if · fix · severity**  
**must** = dual owners (two docs that both decide the same thing), router fork (parallel Start Here tables), broken hub, cache treated as deciding owner for another file’s contract, Prefer/Avoid silently ignored with no fork.  
**should** = section job, stale pointer, dense jargon without a first-use definition, prose a new host editor cannot apply, index silently paraphrasing (cache without the cache mark), commercial hub with no Open cell for a named pitch job (then `engagement-review`).  
**defer** = pure trim of already-clear wording.  
**pushback** = numbered fork to change structure Prefer/Avoid or fail-if (confirm); not a soft skip of the review.
