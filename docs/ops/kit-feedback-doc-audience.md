# Kit feedback — documentation audience (NuGet / wiki hosts)

**Audience:** Railkit maintainers.  
**Host evidence:** SqlInterpol (NuGet package; `docs/` historically synced wholesale to GitHub wiki).  
**Incident class:** Adopt/apply misfit — not a SqlInterpol product bug. Companion host lesson: separate consumer vs contributor documentation homes.

## Fast path

- **Problem:** Railkit adopt injects agent **Fast path / CANONICAL / ops** situating into host topic docs that some hosts publish as **end-user / package / wiki** documentation.
- **Symptom:** Consumer pages gain `src/...` paths, contributor routers, Rank/ops links, and (when hosts also err) internal test harness names.
- **Rule for kit:** distinguish **shipped product docs** vs **contributor situating docs** before writing Fast paths into topic homes.
- **Host (SqlInterpol) fix:** wiki allowlist + `AGENTS.md` / `.cursor/rules/documentation-audience.mdc` (this repo).

## Incident (SqlInterpol apply)

| Signal | What happened |
|--------|----------------|
| Host contract | `docs/*` → GitHub wiki (NuGet users) |
| Kit apply | Fast path blocks + CANONICAL/ops/src links on every product topic page; README Railkit table above product pitch |
| Host product PRs | Contributor proof (`*TestSuite`, call-site harness notes) pasted into `performance-aot.md` |
| Reader impact | Package users see agent situating and repo-internal proof language |

## Kit patch (copy into upstream Railkit)

### 1. `docs/ADOPTION.md` (Document owners / host fill-in)

Add a required host fill-in before specializing product topic Fast paths:

| Field | Purpose |
|-------|---------|
| **Shipped doc root** | Paths that become end-user / package / wiki / site docs |
| **Contributor doc root** | Paths for situating, ranks, adopt, CI how (agents + maintainers) |
| **Publish mechanism** | e.g. wiki sync, DocFX, website — and whether sync is allowlist or `docs/**` |

**Prefer:** put Railkit Fast path / CANONICAL / CODE-FIRST / ops links only under the **contributor** root (or root `AGENTS.md`).  
**Avoid:** injecting situating Fast paths into shipped product topic intros by default.  
**Fail if:** adopt completes with situating links inside the host’s shipped doc root without an explicit “same tree is OK” decision recorded on the adopt receipt.

### 2. `docs/DOCUMENTATION-PRINCIPLES.md`

Add audience rank (or Prefer/Avoid row):

| Prefer | Avoid |
|--------|--------|
| One deciding owner **per audience** when shipped docs ≠ contributor docs | One Fast path that serves both NuGet readers and agent situating |
| Product topic pages answer consumer jobs (install, API, integrate) | `src/` paths, harness class names, kit routers in shipped pages |
| Situating lives in AGENTS / CANONICAL / ops | Rewriting the host README product hero into a kit intent table without a contributor subsection |

### 3. `agents/skills/adopt.md` + `doc-review.md`

- Adopt step: if host publishes docs, **classify each existing `docs/*.md` as shipped vs contributor** before adding Fast paths.
- Doc-review: fail shipped pages that link to CANONICAL/ops/CODE-FIRST or name repo-only tests/`src/` as primary guidance (unless host fill-in says shipped tree includes contributors).

### 4. Templates / example hosts

- Example wiki/DocFX sync should be an **allowlist of consumer pages**, not `cp -r docs/*`.
- README template: keep **product pitch first**; put “Start here by intent (Railkit)” under a **Contributing / agents** heading (or separate `AGENTS.md` only).

## Out of scope for this patch

- Host-specific SqlInterpol wiki filenames.
- Whether Testing.Xunit guides are “consumer” (they are, for that package) vs Rank-2 ops (contributor).

## Acceptance

- A NuGet/wiki host can adopt Railkit without polluting package docs.
- Agents still situate via AGENTS → CANONICAL → contributor owners.
- Doc-review / adopt receipt catches shipped-tree situating leaks.
