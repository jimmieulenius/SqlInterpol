# UI principles

**Purpose:** Ranked Prefer/Avoid for end-user UX conflicts (skill `ui-review`). Portable; strip or specialize host domain rows during adopt.

## Fast path (read first)

- **Conflicts:** [Ranked principles](#ranked-principles) — higher wins.
- **Copy / labels:** [ui-naming.md](ui-naming.md).
- **Layout / regions:** [in-page-layout.md](in-page-layout.md).
- **Living tokens page (optional Rank 7 depth):** [living-design-surface.md](living-design-surface.md).
- **Minor domains:** admin vs end-user may use separate Prefer/Avoid or Moments — not dual homes of one UX rule.
- **Day-to-day:** [Tensions](#tensions) + [Moments](#moments) + [Ship checklist](#ship-checklist).

## Why

Optimize for efficiency: user attention, support load, build energy. Waste = duplicated concepts, ambiguous labels, hidden state, parallel paths, and working memory the UI forces the user to hold.

## Ranked principles

| Rank | Principle | Means in one line | Fail if… |
|------|-----------|-------------------|----------|
| **1** | Correctness and safety | Do not silently do the wrong or irreversible thing | Silent overwrite; fake saved; irreversible delete without friction |
| **2** | Representational truth | Show and name the actual outcome; copy and look stay concrete | Preview ≠ export; abstract synonym when an everyday verb exists; silent state only in the user's head |
| **3** | Path gravity | One obvious path for the common job; rare paths quieter | Equally loud parallel paths for the same outcome |
| **4** | Value per time | Earn every click and pixel | Decorative chrome that must be decoded to act |
| **5** | Steady shell | Keep last-known useful UI while loading | Full-page blank flash on every soft nav |

## Tensions

| Tension | Default lean |
|---------|----------------|
| Density vs clarity | Clarity on first use; densify after mastery |
| Consistency vs local optimum | Shared patterns unless a Moment demands exception |
| Guidance vs Steady | Default cut standing helpers; celebrate rare wins |

## Moments

Moments that tilt tensions: first visit, irreversible action, error recovery, empty state, permission denied, success after a long chore. Name the Moment when breaking a default lean.

## Ship checklist

1. Named deciding rank if two ideas conflict.
2. Labels pass ui-naming Prefer/Avoid.
3. Regions match in-page-layout (or justify exception).
4. No silent state the user must remember.
5. Loading preserves a steady shell where possible.

## Maintaining

Prefer tensions / Moments over new Prefer/Avoid rows. Specialize after repeated drift. Host product words live in ui-naming, not here.
