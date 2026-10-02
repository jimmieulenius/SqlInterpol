# Skill: commit

**Owner:** host commit policy; index [ci-flow.md](../../docs/ops/ci-flow.md)

Use only when the user asks to commit or plan commits. Preview the batch plan; stage explicit paths; conventional messages.

## Loop

1. `git status` / `git diff` / recent log (shared-checkout awareness — other writers may own hunks).
2. Group by outcome; prefer one user-visible outcome per feat/fix/perf if the host uses that bar.
3. Stage explicit paths (not blind `git add -A` on a dirty shared tree).
4. Commit; never skip hooks unless the user explicitly asks.
5. Do not push unless asked.

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| Explicit path staging | Committing foreign hunks (changes you did not make) |
| Message matches host convention | Rewriting pushed history |
