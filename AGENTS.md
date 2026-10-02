# Agent entry (SqlInterpol)

## Documentation audience (read before editing docs)

SqlInterpol is a **NuGet package**. Treat documentation as two products:

| Audience | Home | Synced to wiki? |
|----------|------|-----------------|
| **Consumer** (package users) | Topic guides under `docs/` listed in `.github/workflows/sync-wiki.yaml` | Yes |
| **Contributor** (improve package / agent situating) | This file, `.cursor/rules/`, `TODO.md`, PR text; with Railkit: `docs/ops/`, `docs/engineering/`, `docs/CANONICAL-SOURCES.md`, kit meta docs | No (must stay off the wiki allowlist) |

**Rule:** In consumer/wiki pages, document *what the consumer observes* and *which public API/package to use*. Do **not** put `src/...` paths, host test suite/class names, Spec/Shared hosting notes, Railkit situating, ratchets, or CODE-FIRST there.

**When behavior ships:** consumer-visible notes go in the matching topic guide; proof/harness/how-we-tested goes in the PR, `TODO.md`, or contributor docs.

Cursor rule (always on): `.cursor/rules/documentation-audience.mdc`.

## Code work

- Prefer Spec / existing host harnesses over parallel twin test suites (`docs/testing-mocking.md` for consumer Testing.Xunit usage; contributor Rank-2 how lives with Railkit ops when present).
- Do not invent a second documentation tree for the same audience.
