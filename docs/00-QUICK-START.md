# Quick start (install Railkit into a host)

**Purpose:** Merge this governing framework into an existing codebase so agents and humans share one situating path (open the one right owner doc before edit) and the same tighten-only quality checks.

**Source:** copy from the Railkit monorepo `kit/` folder only. Never copy `project/` (offer, marketing, competition, consulting).

## Fast path (read first)

1. Run `node kit/scripts/install/copy-kit-to-host.js --target <hostDir>` (or copy per [What to copy](#what-to-copy)).
2. Point the host intent router at Railkit rows — use [templates/host-readme-stub.md](../templates/host-readme-stub.md); **do not** keep kit `README.md` (it links monorepo `project/`).
3. Set `architecture-budgets.json` `roots` to host code — examples: `["src"]`, or `["apps","packages"]` for monorepos (kit defaults scan kit tree only). See [code-quality-and-refactor.md](ops/code-quality-and-refactor.md#architecture-roots-examples).
4. Wire `package.json` check scripts to the installed script paths; run [ADOPTION.md](ADOPTION.md).
5. Rank **7:** wire `check:unused-export` (`queued` with owner+due, or `knip`) — not part of foundation `check:railkit` until you choose.

## What to copy

| From `kit/` | Into host |
|-------------|-----------|
| `docs/` (hubs, ops, engineering, product/heuristics) | `docs/` (merge; keep host product specs) |
| `agents/` | `agents/` (canonical skills) |
| `adapters/cursor/` | `.cursor/` after reading [adapters/cursor/README.md](../adapters/cursor/README.md) |
| `adapters/README.md` | `adapters/README.md` (adapter contract index) |
| `templates/agent-session-rules.md` | Host `.cursorrules` / agent rules (adapt names) |
| `templates/host-readme-stub.md` | Host `README.md` if missing (specialize) |
| `templates/rank7-dated-queue.md` | Host `docs/ops/rank7-dated-queue.md` (install script copies) |
| `scripts/` | `scripts/railkit/` (default) or merge with `--scripts-merge` |
| `infra/contracts/example-hosting-contract.json` | Start host `infra/contracts/` |
| `llm.txt`, `AGENTS.md` | Root |
| Monorepo `.github/workflows/railkit-checks.yml` | Host workflows (rename; `working-directory` = host root) |

Do not copy monorepo `project/` or kit `README.md` as the host product hub.

## Scripts layout

Check scripts resolve the repo root by walking up to `docs/ADOPTION.md` + `package.json` ([`scripts/lib/repo-root.js`](../scripts/lib/repo-root.js)). Both layouts work:

- **Nested (default):** `scripts/railkit/documentation/…` and `scripts/railkit/architecture/…`
- **Merge:** `scripts/documentation/…` and `scripts/architecture/…`

Example `package.json` scripts for nested layout:

```json
{
  "scripts": {
    "check:documentation": "node scripts/railkit/documentation/check-documentation.js",
    "check:documentation:ownership": "node scripts/railkit/documentation/check-documentation-ownership.js",
    "check:architecture": "node scripts/railkit/architecture/check-architecture.js",
    "check:architecture:ratchet": "node scripts/railkit/architecture/check-architecture-ratchet.js",
    "check:unused-export": "node scripts/railkit/unused-export/check-unused-export.js",
    "check:railkit": "npm run check:documentation && npm run check:documentation:ownership && npm run check:architecture && npm run check:architecture:ratchet"
  }
}
```

After Rank **1**–**2** hold, add `&& npm run check:unused-export` to the umbrella when `unused-export.json` mode is `knip` (or keep `queued` with a live clock).

## Prerequisites

- Node 20+ if using the check scripts.
- An editor or agent that can load markdown skills (Cursor adapter included; others: [adapters/README.md](../adapters/README.md)).

## Verify

```bash
npm run check:railkit
```

Then open [ADOPTION.md](ADOPTION.md) and run the bootstrap sequence on the host's real docs. Dogfood reference: monorepo [`examples/first-host/`](../../examples/first-host/) + [adopt-receipt.md](../../examples/first-host/docs/ops/adopt-receipt.md).
