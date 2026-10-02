# Skill: layering-review

**Owners:** [application-layering.md](../../docs/engineering/architecture/application-layering.md), [code-quality-and-refactor.md](../../docs/ops/code-quality-and-refactor.md)

Default bias: adjust the application toward the yardstick.

## Efficient pass

Subject sentence (`placement:` / `net-simpler:`). Seam-only: start at route/coordinator/store. Smoke architecture / unused-export checks when in scope.

## Loop

1. Thin shells (HTTP/UI parse and orchestrate only); persistence in domain; one coordinator for writes.
2. Placement: domain folders; first-pass feature folders; consolidation deletes duplication.
3. Net simpler: delete unused scaffolding; require named owner destination for "for later" shape.
4. Exit: ratchet (size or quality budget) only tightens; add or update tests when logic/behavior moves ([testing-bar.md](../../docs/ops/testing-bar.md)).

## Severity

**must** = second write path; SQL/DB in shell. **should** = unused wrapper; hub pulling heavy graphs into light readers.
