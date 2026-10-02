# Customer fit

**Purpose:** Ranked Prefer/Avoid for feature and claim fit against [customer-targets.md](customer-targets.md).

## Fast path (read first)

- Open [customer-targets.md](customer-targets.md) Who / job first.
- Decide fit with ranks below; do not invent a parallel who-table here.
- Skill: `fit-review`.

## Ranks

| Rank | Principle | Fail if… |
|------|-----------|----------|
| **1** | Serves a named Primary job (WYSIWYG safe SQL, dialect render, integrations) | Feature only for an unnamed future segment |
| **2** | Strengthens the product spine (builder → pipeline → dialect → execute) | Off-spine admin/tooling that does not feed query building or safety |
| **3** | Value per time for the library consumer | Heavy process for a thin outcome |
| **4** | Claim matches shipped behavior | Marketing promise without an owner spec or tests |

## Open when

| Need | Open |
|------|------|
| Who / Primary job | [customer-targets.md](customer-targets.md) |
| Module placement | [application-layering.md](../../engineering/architecture/application-layering.md) |
| Doc dual home | [DOCUMENTATION-PRINCIPLES.md](../../DOCUMENTATION-PRINCIPLES.md) |

## Prefer / Avoid

| Prefer | Avoid |
|--------|--------|
| Features that complete interpolate → parameterize → dialect-render → execute | Parallel query APIs that bypass `SqlBuilder` without a removal plan |
| Explicit `Sql.Raw` / opt-in escape hatches | Silent string concat that reintroduces injection risk |
| Integration packages that adapt core results | Re-implementing pipeline logic inside Dapper/EF packages |
| Analyzer/generator help that matches runtime rules | Diagnostics that disagree with runtime dialect gates |
| Claims backed by specs under `tests/` | README claims without a suite or dated gap |
