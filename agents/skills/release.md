# Skill: release

**Owner:** [ci-flow.md](../../docs/ops/ci-flow.md) release section (host fills)

Use when the user asks to release, ship, tag, or deploy. Until the host documents release commands, stop and ask for the host runbook to be written as code ([CODE-FIRST.md](../../docs/CODE-FIRST.md)).

## Loop

1. Confirm release is requested.
2. Run host pre-release checks (same as CI where possible).
3. Execute the host release script/workflow (not ad-hoc portal steps — click-only ops outside versioned scripts).
4. Persist until deploy verification the host defines.

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| Versioned release script / workflow | Click-only production changes |
| Diary / changelog rules the host owns | Inventing a release process in chat |
