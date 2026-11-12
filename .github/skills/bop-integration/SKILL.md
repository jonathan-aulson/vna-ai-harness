---
name: bop-integration
description: 'Specification-first workflow for BOP integration stories. Uses the CM Implementation Specification as the governing CM/BOP contract, enforces ownership boundaries, endpoint/schema decision checks, cross-story dependency checks, BOP follow-ups, and AC coverage reconciliation.'
---

# BOP Integration Skill

## Governing Contract

- For every BOP integration story and task, `backend/src/Infrastructure/docs/CM Implementation Specification.md` is the governing CM/BOP implementation contract.
- If story acceptance criteria conflict with that specification, implementation follows the specification.
- Conflicts must be raised to the user before implementation and again after implementation so AC can be updated.

## Scope Anchor

- This skill formalizes the approved recommendation from task 10771 under story 10500.
- Use it for all follow-on BOP stories that touch CM endpoints, payloads, status handling, schema, sync/retry behavior, or BOP/CM responsibilities.

## When To Use

- Story/task text references BOP, CM, client-sites, client-issues, meal orders, delivery feedback, driver survey, or BOP synchronization.
- Work changes or validates CM API contracts consumed by BOP.
- Work depends on decisions documented in the CM Implementation Specification decision log references.

## Specification-First Planning Workflow

1. Load the story AC and task scope.
2. Load the governing specification and identify applicable sections and decisions.
3. Build a conflict matrix before implementation:
   - Story AC item
   - Spec section or decision reference
   - Match status: `Match`, `Conflict`, or `Gap`
   - Implementation source of truth
   - Required user action
4. If any `Conflict` or `Gap` exists, stop and request user confirmation before coding.
5. Implement to the specification once approved, then map final behavior back to AC coverage.

## CM and BOP Ownership Boundaries

- CM owns:
  - CM-side endpoint behavior, payload shapes, and field derivations.
  - CM-side persistence changes, EF model/configuration, migrations, and validation.
  - CM-side business rules described as CM obligations in the governing specification.
- BOP owns:
  - BOP runtime ingestion behavior and deactivation/retry execution behavior on its side.
  - BOP-side guards and follow-up implementation required to consume CM contracts safely.
  - BOP-side processing changes explicitly identified as external dependencies.

## Endpoint and Schema Decision Checks

Before implementation, verify and record:

- Endpoint shape and envelope decisions (paging, response wrappers, status fields, deterministic ordering).
- Field naming decisions and discriminator rules.
- Effective-date versus latest-row semantics where specified.
- Data-type, nullability, length, and precision constraints for any changed fields.
- Whether a decision is marked decided versus proposed and whether that affects delivery risk.

## Cross-Story Dependency Checks

- Identify dependencies between current story scope and prerequisite stories, tasks, or columns.
- Capture explicit prerequisites that block correctness even when code compiles.
- Include cross-story references when relevant (for example, story 10500 contract dependencies and follow-up stories such as 10613 and 10681 noted in the governing specification).

## Required BOP-Side Follow-Ups

- Produce a required BOP follow-up list whenever CM behavior relies on BOP-side handling.
- Include why each follow-up is required, impact if missing, and who owns the follow-up.
- Treat unresolved BOP follow-ups as delivery risks; do not hide them inside CM-only completion notes.

## Acceptance Criteria Coverage

- Map each AC item to one of: `Implemented`, `Implemented via Spec Override`, `Deferred`, or `Blocked`.
- `Implemented via Spec Override` requires:
  - pre-implementation user confirmation,
  - post-implementation reminder to update AC wording,
  - explicit note of spec section or decision that governed implementation.
- Never silently narrow, reinterpret, or skip AC behavior because existing code differs.