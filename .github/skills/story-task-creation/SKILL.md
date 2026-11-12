---
name: story-task-creation
description: "Use when creating child tasks for a user story, breaking down stories into implementation tasks, planning FE/BE/DATA work streams, or validating task coverage against acceptance criteria. Produces draft task plans with AC coverage matrix and duplicate detection."
argument-hint: "storyId=<required> project=<optional, default mow 2.0>"
---

# Story Task Creation

## Purpose

Systematically decompose an Azure DevOps user story into evidence-based child tasks across implementation streams (`[BE]`, `[FE]`, `[DATA]`), validate full acceptance criteria coverage, detect duplicates, and gate on human confirmation before creation.

## Inputs

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `storyId` | Yes | — | Azure DevOps User Story work item ID |
| `project` | No | `mow 2.0` | ADO project name |
| `externalContext` | No | — | Additional context (architecture notes, constraints) |

## Workflow

### Step 1 — Fetch Story Context

Use ADO MCP to retrieve the user story by ID. Extract:
- Title and description
- All acceptance criteria (numbered list, preserve exact wording)
- Existing child tasks (ID, title, state)
- Parent feature/epic (if linked)
- Sprint/iteration context

### Step 2 — Analyze Stream Evidence

Examine the story description and acceptance criteria to identify which streams require tasks.

**Stream detection heuristics:**

| Signal Keywords | Stream |
|----------------|--------|
| endpoint, API, handler, service, command, query, integration, webhook, validation logic, authorization | `[BE]` |
| page, UI, component, form, displays, navigation, modal, toast, table, filter, user sees, user clicks | `[FE]` |
| migration, schema, entity, column, index, seed, database, table structure, constraint | `[DATA]` |

Rules:
- Only draft tasks for streams where clear evidence exists in the ACs or description.
- A single AC may produce tasks in multiple streams (e.g., "user submits form and data is persisted" → FE + BE + DATA).
- If no evidence exists for a stream, do NOT create tasks for it.

### Step 3 — Draft Task Plan

For each identified stream, draft tasks following the `task-description-standards` skill:
- **One task per stream by default** (`[DATA]`, `[BE]`, `[FE]`) — target 2–4 tasks per story. Split a stream only when it spans multiple modules, contains an early-merge dependency for another stream, or its checklist exceeds ~10 items.
- Each task description contains an ordered **work-item checklist** of the domain elements it covers (endpoints, command/query flows, schema changes, form flows), each mapped to its AC numbers.
- Use descriptive, solution-oriented language.
- Include all five required description sections.
- Map each task (and each checklist item) to specific acceptance criteria by number.

After drafting the task set, assign one story-level `Effort` estimate using only Fibonacci values: `1`, `2`, `3`, `5`, `8`, `13`, `21`.
- Base `Effort` on actual implementation complexity for the full user story, not on an assumed reviewer preference.
- Do not assign `Effort` to child tasks.

### Step 4 — Build AC Coverage Matrix

Create a matrix showing coverage status for every acceptance criterion:

```
| AC # | Criterion (abbreviated) | Status | Covering Tasks |
|------|------------------------|--------|----------------|
| 1 | ... | Covered | [BE] Task A, [FE] Task B |
| 2 | ... | Covered | [DATA] Task C |
| 3 | ... | Partial | [BE] Task D (missing FE) |
| 4 | ... | Blocked | — (dependency on external API) |
```

**Status definitions:**
- `Covered` — At least one drafted task fully addresses this criterion.
- `Partial` — Some aspect is addressed but gaps remain. Specify what's missing.
- `Blocked` — Cannot be addressed without external resolution. Specify blocker.

### Step 5 — Detect Duplicates

Compare drafted task titles against existing child tasks using normalized matching:

1. Normalize: lowercase, strip punctuation, remove stop words, remove stream tag.
2. Compare each draft against each existing child task title.
3. Flag as potential duplicate if normalized forms share >60% token overlap.

### Step 6 — Evaluate Readiness

**BLOCK creation if:**
- Any acceptance criterion has status `Partial` or `Blocked`.
- Present the gaps and ask the user how to resolve before proceeding.

**WARN and recommend skip if:**
- Potential duplicates are detected.
- Present the overlapping pairs and ask the user to confirm skip or override.

### Step 7 — Request Confirmation

Present the complete draft plan and ask for explicit approval.

Accepted confirmation phrases: `Create these tasks`, `yes`, `approved`, `go ahead`, `create them`.

Do NOT proceed to creation without one of these confirmations.

### Step 8 — Create Tasks

On approval:
1. Create each task as a child work item under the parent story via ADO MCP.
2. Set work item type to `Task`.
3. Set state to `New`.
4. Leave `Assigned To` empty (unassigned).
5. Set title with stream tag prefix.
6. Set description with the five required sections.
7. Update the parent user story's ADO `Effort` field to the drafted Fibonacci estimate.

### Step 9 — Report Results

Return creation results with:
- Task ID, title, and link for each created task.
- Any creation failures with error details.
- Final AC coverage confirmation.

## Return Format

Structure the output in these sections (in order):

### 1. Story Snapshot
US ID, title, description summary, total AC count, existing child task count, proposed story `Effort`.

### 2. Draft Task Plan By Stream
Tasks grouped under `[BE]`, `[FE]`, `[DATA]` headers with full description content.

### 3. Acceptance Criteria Coverage Matrix
The matrix from Step 4.

### 4. Duplicate Warnings and Skip Recommendations
Any detected overlaps with existing child tasks. Empty if none found.

### 5. Confirmation Required
Explicit gate — wait for user approval.

### 6. Creation Result
Only populated after confirmation. Task IDs, titles, updated parent story `Effort`, links, and any errors.

## Error Handling

- If the story ID is not found, report the error and stop.
- If ADO MCP is unavailable, report the error and stop.
- If a task creation fails, report it and continue with remaining tasks.
- Never retry failed creations automatically — report and let the user decide.
