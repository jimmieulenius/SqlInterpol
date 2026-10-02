# Skill: stability-review

**Owner:** [ongoing-engineering-bar.md](../../docs/ops/ongoing-engineering-bar.md) (Shared contracts / Side effects)

Not a hotspot size sweep (use layering-review).

## Efficient pass

Subject sentence (`stability:` / `contract:`). One skill per turn. Same-turn must/should fixes.

## Loop

1. Name the symptom (works on A, missing on B; stale apply; double write).
2. Name the SSOT contract (single source of truth); route failing surfaces through it.
3. Side effects: coordinator owns order; leaves trigger intent only.
4. Clocks / versions: no silent overwrite of newer state.

## Severity

**must** = parallel write path; leftover fork that disagrees with SSOT. **should** = missing invalidation; efficiency fork that reimplements priority.
