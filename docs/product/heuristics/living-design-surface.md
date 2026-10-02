# Living design surface

**Purpose:** Pattern note for an optional host yardstick page (tokens, components, motion). Reference shape: an internal `/admin/design`-style route. Railkit does not ship the page.

## Fast path (read first)

- Heuristics live in [ui-principles.md](ui-principles.md); the living page shows tokens/components in code.
- Build the page only when the host has a web UI and a token source.
- Code-first: tokens in repo CSS/variables; the page reads them, it does not invent a second palette in Markdown.

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| One route that mirrors production tokens | Screenshot wiki as the design single source of truth |
| Components rendered from the real library | Parallel Storybook that drifts from app imports |
| Link from ui-principles Fast path when the page exists | Requiring the page before adopt can start |

## Adopt

Queue "add living design surface" with owner + due date when UI heuristic docs are enabled. Do not block bootstrap on it.
