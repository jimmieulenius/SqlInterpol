# Customer targets

**Purpose:** Who we serve and Primary jobs. Skill `fit-review` reads this before [customer-fit.md](customer-fit.md).

## Fast path (read first)

- Primary buyer: .NET developers shipping SQL against real databases.
- Primary job: write WYSIWYG, parameterized, dialect-aware SQL with compile-time safety.
- Not buyers: teams seeking a full ORM replacement, a visual query designer, or a hosted SaaS.

## Who / job

| Segment | Primary job | Not these buyers |
|---------|-------------|------------------|
| Library consumer (Dapper / ADO.NET / EF Core app) | Build and execute type-safe interpolated SQL with automatic parameters and dialect render | Teams that want SqlInterpol to own migrations, change tracking, or UI |
| Extensibility author (custom dialect / rewriter) | Plug a dialect or rewriter without forking core | Teams expecting plugin marketplace / hosted extension runtime |
| Maintainer / agent editor | Change core or integrations with clear owners and proof | Editors using a second agent-only rulebook beside this repo |

## App core (index)

| Concern | Owner |
|---------|--------|
| Product entry / install | [getting-started.md](../../getting-started.md) |
| Fit Prefer/Avoid | [customer-fit.md](customer-fit.md) |
| Module boundaries | [application-layering.md](../../engineering/architecture/application-layering.md) |
