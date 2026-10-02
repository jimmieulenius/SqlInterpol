# CI helpers

Optional host wiring (Husky, pre-push). Not required for Railkit v1.

## Prefer

- Pre-commit: fast lint / staged checks only
- Pre-push or CI: `npm run check:railkit`
- Same commands in [`.github/workflows/railkit-checks.yml`](../../.github/workflows/railkit-checks.yml)

## Avoid

- Remote-only scripts humans cannot run
- Skipping hooks without an explicit ask
