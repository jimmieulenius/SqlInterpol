# Tribal inventory

**Purpose:** Rank **5** list of repeatable how that might live only in heads, chat, or wiki — and where it lives now (or Rank **7** clock).

## Fast path (read first)

- Inventory must exist for adopt exit.
- At least one process moved to code **or** dated-queued.
- Owner: [CODE-FIRST.md](../CODE-FIRST.md); clocks: [rank7-dated-queue.md](rank7-dated-queue.md).

| Process | Was tribal? | Now | Action |
|---------|-------------|-----|--------|
| Product build / unit test | No | `.github/workflows/ci.yaml` + `dotnet test SqlInterpol.slnx` | Keep; documented in [ci-flow.md](ci-flow.md) |
| Railkit governing checks | Was missing | `package.json` + `railkit-checks.yml` | Shipped this adopt |
| Multi-DB e2e | No | `e2e-databases.yml` + `docker-compose.e2e.yml` | Keep |
| NuGet / semantic release | No | `cd.yaml` + `.releaserc.yaml` | Keep |
| Wiki sync | No | `sync-wiki.yaml` | Keep |
| Informal backlog (`TODO.md`, `PLAN.md`) | Partial | Root markdown, not skills/CI | Dated: either promote items into issues/CANONICAL tasks or delete when stale — owner sqlinterpol-maintainers, due **2026-11-01** |
| Portal-only NuGet/GitHub settings | Maybe | Not fully inventoried | Queued under vendor assert/apply due **2026-12-01** ([rank7-dated-queue.md](rank7-dated-queue.md)) |

**Moved to code this adopt:** Railkit check scripts + workflow + CI mental model in [ci-flow.md](ci-flow.md).
