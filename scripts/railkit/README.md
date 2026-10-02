# Scripts

Executable how for Railkit gates and install. Docs link here ([CODE-FIRST.md](../docs/CODE-FIRST.md)).

| Script | npm / usage |
|--------|-------------|
| `documentation/check-documentation.js` | `check:documentation` |
| `documentation/check-documentation-ownership.js` | `check:documentation:ownership` |
| `architecture/check-architecture.js` | `check:architecture` |
| `architecture/check-architecture-ratchet.js` | `check:architecture:ratchet` |
| `unused-export/check-unused-export.js` | `check:unused-export` (Rank **7**; `queued` or `knip`) |
| `lib/repo-root.js` | Shared root walk (supports `scripts/railkit/` layout) |
| `install/copy-kit-to-host.js` | `node scripts/install/copy-kit-to-host.js --target <host>` |
| Foundation umbrella | `check:railkit` (docs + architecture; not unused-export) |

CI: [`.github/workflows/railkit-checks.yml`](../../.github/workflows/railkit-checks.yml). Dogfood host CI: [`first-host-checks.yml`](../../.github/workflows/first-host-checks.yml). Dated queue template: [templates/rank7-dated-queue.md](../templates/rank7-dated-queue.md).
