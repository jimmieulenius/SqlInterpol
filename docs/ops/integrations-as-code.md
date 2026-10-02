# Integrations as code

**Purpose:** Apply [CODE-FIRST.md](../CODE-FIRST.md) to hosting, DNS, edge, and vendor wiring. Console clicks are debt unless a contract and script point at them.

## Fast path (read first)

- Put desired state in [`infra/contracts/`](../../infra/contracts/) (JSON/YAML).
- Prefer assert/apply scripts under `infra/scripts/` (owned by the host).
- Docs explain why and link; they do not replace the contract.
- During adopt: list portal-only steps; script them or give them an owner and due date ([ADOPTION.md](../ADOPTION.md)).

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| Versioned hosting/DNS/env contract | "We configured it in the console once" |
| Scripts that assert required env keys / hostnames | Runbooks that only list clicks with no assert |
| Secrets in the secret store; names in the contract | Secrets committed to git |
| One vendor integration owner module or script folder | Copy-pasted curl snippets in three READMEs |

## Example

See [`example-hosting-contract.json`](../../infra/contracts/example-hosting-contract.json). Replace with the host's cloud and DNS provider. Keep the idea: machine-readable desired state + code that applies or checks it.
