# CI flow

**Purpose:** Mental model for cheap local checks → clean-room CI → release. Commands live in `package.json`, `dotnet`, and workflows ([CODE-FIRST.md](../CODE-FIRST.md)).

## Fast path (read first)

- **Product (must for behavior):** `dotnet test SqlInterpol.slnx -c Release` (same as [`.github/workflows/ci.yaml`](../../.github/workflows/ci.yaml))
- **Railkit governing:** `npm run check:railkit` (same as [`.github/workflows/railkit-checks.yml`](../../.github/workflows/railkit-checks.yml))
- **Rank 7 unused API:** `npm run check:unused-export` (queued until .NET gate ships — [rank7-dated-queue.md](rank7-dated-queue.md))
- **Skill:** `ci-gate` — what to run when merge fails or before push
- **Do not:** invent remote-only gates that humans cannot run locally

## Mental model

| Stage | Role |
|-------|------|
| **Editor / pre-commit (optional)** | Fast lint or staged checks; wire later if needed |
| **Local product** | `dotnet restore` / `build` / `test` on `SqlInterpol.slnx` |
| **Local Railkit** | Doc ownership + file-size ratchets (`check:railkit`); unused-export via `check:unused-export` when mode leaves `queued` |
| **CI workflows** | `ci.yaml` = product build/test; `railkit-checks.yml` = governing scripts; `e2e-databases.yml` = heavier DB proof; `cd.yaml` = release |
| **Release** | Semantic-release + CD workflow (already code-first) — skill `release` when shipping |

## Host fill-in

| Item | Value |
|------|--------|
| Default branch | `main` |
| Product solution | `SqlInterpol.slnx` |
| .NET CI | PR to `main` → build + test Release |
| E2E | `e2e-databases.yml` + `docker-compose.e2e.yml` (path-triggered / scheduled as configured) |
| Governing CI | `railkit-checks.yml` on push/PR |
| Shared checkout | Skill `shared-checkout` — skip foreign hunks; no clobber |

**Trend gates / tests:** instruments of [governing-priorities.md](governing-priorities.md). Adopt order: [ADOPTION.md](../ADOPTION.md#correct-sequence), [testing-bar.md](testing-bar.md).

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| One mental merge bar: product green **and** Railkit green | Divergent laptop vs CI commands |
| Rank **1**–**2** held before chasing Rank **7** instruments | Skipping owners to wire unused-API theater first |
| CI as how Rank **3**/**4** bind every editor | Treating CI jobs as goals above situating |
| Unit + e2e as Rank **2** proof vehicles | Merging behavior changes with red or skipped tests |
| Path-triggered heavy e2e when possible | Running the universe on every docs typo |
