# Infra

Hosting, DNS, edge, and vendor **contracts** live here as code ([docs/CODE-FIRST.md](../docs/CODE-FIRST.md), [docs/ops/integrations-as-code.md](../docs/ops/integrations-as-code.md)).

| Path | Role |
|------|------|
| [contracts/](contracts/) | Desired-state JSON/YAML |
| `scripts/` (host-owned) | Assert / apply against providers |

Railkit ships an example contract only. Do not commit secrets.
