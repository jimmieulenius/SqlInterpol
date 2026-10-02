# Editor adapters

Policy and Prefer/Avoid live in `docs/` and `agents/skills/`. Adapters are thin wrappers only.

## Contract

| Prefer | Avoid |
|--------|--------|
| Short must-dos + links to owners | Second policy tree in the editor folder |
| Skill frontmatter that points at `agents/skills/*.md` | Copying Prefer/Avoid tables into adapter bodies |
| Few always-on rules | Fat always-on dumps of 400-line docs |

## Included

- [cursor/](cursor/README.md) — Cursor rules + SKILL.md wrappers

## Other tools (wiring note)

Any agent that can load markdown:

1. Point session rules at [templates/agent-session-rules.md](../templates/agent-session-rules.md) or [llm.txt](../llm.txt).
2. Load skill bodies from [agents/skills/](../agents/skills/) by name when the user invokes that intent.
3. Do not fork policy into tool-specific prose.

Examples: Claude Code custom commands, Copilot custom instructions, and similar can symlink or `@`-include `agents/skills/<name>.md`. Full ports are out of Railkit v1 scope.
