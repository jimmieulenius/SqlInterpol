# Skill: ci-gate

**Owner:** [ci-flow.md](../../docs/ops/ci-flow.md)

Pick what to run when CI failed, before merge, or before push. Do not invent remote-only commands.

## Loop

1. Name the failure or the question ("safe to merge?").
2. Prefer `npm run check:railkit` (or host umbrella) first.
3. If the host has resume flags / peel manifests (ways to skip already-green prefixes), use them; document in ci-flow.
4. Heavy suites only when paths or the failure warrant them.

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| Same entrypoint as CI | One-off bash that CI does not run |
| Resume after green prefix | Full rebuild theater every time |
