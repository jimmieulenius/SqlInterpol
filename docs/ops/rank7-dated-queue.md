# Rank 7 dated queue

**Purpose:** Named ideal instruments with owner + due. Undated / ownerless rows fail Rank **7**.

## Fast path (read first)

- One row per instrument.
- Owner + due required.
- Slipped dates re-enter the working loop ([ADOPTION.md](../ADOPTION.md#gradual-change)).

| Instrument | Serves ranks | Owner | Due (YYYY-MM-DD) | Status |
|------------|--------------|-------|------------------|--------|
| Unused-export / .NET public API gate | 4, 6, 7 | sqlinterpol-maintainers | 2026-11-15 | queued (`scripts/railkit/unused-export/unused-export.json`) |
| Host unit+e2e on CI | 2, 4, 7 | sqlinterpol-maintainers | 2026-10-02 | shipped — `.github/workflows/ci.yaml` + `e2e-databases.yml` (keep named in [ci-flow.md](ci-flow.md)) |
| UI heuristics + one ui-review | 7 | sqlinterpol-maintainers | 2026-10-02 | n/a — no product UI (library host) |
| Auth / entitlement owners | 1, 7 | sqlinterpol-maintainers | 2026-10-02 | n/a — no auth product surface |
| AOT honors CrossDialectSqlTranspilation (e.g. UPSERT) | 2, 7 | sqlinterpol-maintainers | 2026-11-15 | queued — open product item in root `TODO.md` |
| Vendor assert/apply | 5, 7 | sqlinterpol-maintainers | 2026-12-01 | queued — NuGet/GitHub release already scripted; portal-only leftovers TBD in [tribal-inventory.md](tribal-inventory.md) |

Yardstick: [governing-priorities.md](governing-priorities.md). Mechanical unused-export gate: `npm run check:unused-export`. Worked chain: [rank-chain-example.md](rank-chain-example.md).
