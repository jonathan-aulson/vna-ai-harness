---
name: task-description-standards
description: "Use when drafting or validating Azure DevOps Task content for story implementation planning. Enforces descriptive, solution-focused task descriptions, title tag conventions, and non-code task language."
---

# Task Description Standards

Apply these rules whenever creating or validating story implementation tasks.

## Title Convention

Task titles must start with exactly one area tag:

| Tag | Stream |
|-----|--------|
| `[BE]` | Backend — API endpoints, handlers, domain logic, integrations |
| `[FE]` | Frontend — UI components, pages, routing, state management |
| `[DATA]` | Data — EF migrations, schema changes, seed data, query optimization |

**Format**: `[TAG] Concise action-oriented title`

Examples:
- `[BE] Implement eligibility check endpoint for payer authorization`
- `[FE] Add referral intake form with validation and submission`
- `[DATA] Create referral entity migration with status enum`

## Required Task Description Sections

Each task description must clearly include:

### 1. Problem Being Solved
What gap or requirement does this task address? Reference the acceptance criterion it maps to.

### 2. Solution Intent
What approach will be taken at a high level? Describe the architectural pattern, not the code.

### 3. Expected Outcome
What is the observable result when this task is complete? Describe it in terms a reviewer can verify.

### 4. Dependencies and Handoffs
What must be complete before this task starts? What does this task unblock for other streams?

### 5. Done Criteria
Concrete, verifiable statements that determine when this task is complete. Must be testable.

## Prohibited Content

Do NOT include in task descriptions:
- Code snippets or inline code
- Pseudocode or algorithm steps
- Class names, method signatures, or file paths
- Low-level implementation instructions (e.g., "add a private field", "inject ILogger")
- Tool-specific commands (e.g., "run dotnet ef migrations add")

## Skill-Aware Planning Guardrails

When a task depends on discovery performed by another skill or agent:

- Keep the description solution-focused: describe the problem, intended outcome, dependencies, and done criteria.
- Do NOT reference specific MCP calls, query text, or tool invocations used during discovery.
- Do NOT embed session-specific context (file paths, branch names) that will become stale.

## Acceptance Criteria Alignment

- Every task MUST map to at least one acceptance criterion or explicit integration dependency.
- Task sets MUST collectively account for ALL acceptance criteria in the parent story.
- If any criterion is not fully covered, mark as `Partial` or `Blocked` and resolve before write operations.
- A single acceptance criterion MAY require tasks across multiple streams.

## Creation Expectations

- Child tasks MUST be created under the specified parent user story.
- Tasks MUST remain unassigned by default unless assignment is explicitly requested.
- Potential duplicates MUST be warned and skipped by default when normalized semantic signals indicate overlap with existing child tasks.
- Task state MUST be set to `New` on creation.
- Task plans MUST NOT assign story points or `Effort` values to child tasks.
- The parent user story MUST carry the Fibonacci `Effort` estimate using only `1`, `2`, `3`, `5`, `8`, `13`, `21`.
- The parent story `Effort` must reflect actual implementation complexity for the full story as drafted, not a guessed human preference.
