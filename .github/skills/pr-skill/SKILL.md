---
name: pr-skill
description: 'PR description templates, conventional commit message format, linking PRs to ADO work items, assigning reviewers, setting draft vs ready state, and documenting plain-language summaries, AC coverage, and created test cases.'
---

# PR Skill

## Repository PR Templates

Three purpose-built templates live in `.github/PULL_REQUEST_TEMPLATE/`. Select the right one when creating a PR in Azure DevOps by appending `?templateId=<name>` to the PR creation URL.

| Template | File | URL param | Use when |
|----------|------|-----------|----------|
| Feature  | `feature.md` | `?templateId=feature` | Merging a feature branch (whole User Story) into `develop` — covers full AC table, per-layer changes, screenshots |
| Bug fix  | `bug.md`     | `?templateId=bug`     | Any bug fix regardless of branch target |

### PR Description Content Requirements

Regardless of template used, every PR description **must** include:

- **Plain-language summary** — 1-2 sentences in business-friendly language
- **Work item links** — `AB#{task-id}` for every task in the story, and `AB#{us-id}` (parent story, for traceability)
- **AC coverage table** — every AC item this PR addresses, with PASS/PARTIAL status and evidence
- **Test cases created or updated** — name and type (unit / integration / ui / manual)
- **Reviewer notes** — areas needing focused review, known gaps, or deferred work

### Feature PR (feature branch → develop)

Use `?templateId=feature`. Key sections to complete:

```
## Feature Summary       — business value, AB#{us-id}
## AC Coverage           — full AC table for the story, all rows checked
## Changes Made          — per-layer breakdown (Backend / Frontend / Data)
## How to Test           — end-to-end steps with environment and data setup
## Screenshots           — before/after for any UI changes (required)
## Risk & Rollback       — risk level + rollback steps
## Self-Review Checklist — all boxes checked
## Reviewer Notes
```

### Bug PR

Use `?templateId=bug`. Key sections to complete:

```
## Bug Summary           — incorrect vs correct behavior, AB#{work-item-id}
## Root Cause            — underlying cause
## Steps to Reproduce    — before-fix reproduction steps
## Fix Description       — what changed
## Regression Test       — test added to prevent recurrence
## How to Verify         — post-fix verification steps
## Risk & Rollback
## Self-Review Checklist
```

---

## Conventional Commit Format

```
type(scope): description

[optional body explaining motivation]

[optional footer]
AB#{work-item-id}
```

### Types

| Type | Use |
|------|-----|
| feat | New feature |
| fix | Bug fix |
| refactor | Code change that neither fixes nor adds |
| test | Adding/updating tests |
| docs | Documentation only |
| chore | Build, CI, dependency updates |
| style | Formatting, whitespace |
| perf | Performance improvement |

### Scope

Use the module or feature name: `feat(referrals): add eligibility verification endpoint`

---

## PR Title Format

PR titles follow conventional commit format: `type(scope): short description`

| Type | Use |
|------|-----|
| `feat` | New feature or user-facing capability |
| `fix` | Bug fix |
| `chore` | Build, CI, configuration, or governance changes |
| `docs` | Documentation only |
| `refactor` | Code restructuring with no behavior change |
| `test` | Test additions or updates |

Examples:
- `feat(referrals): add eligibility verification endpoint`
- `fix(scheduling): correct route assignment on depot change`
- `chore(governance): add repository PR templates`

---

## Merge Strategy

| PR type | Strategy | Commit message format |
|---------|----------|-----------------------|
| Task → feature branch | Squash and merge | `Task {task-id}: {description}` |
| Feature → develop | Squash and merge | `Feature {us-id}: {description} [{feature-branch}]` |
| Develop → Master | Standard merge or rebase | Tag with semantic version (e.g., `v1.2.0`) |

---

## Reviewer Assignments

### Task PRs (task/... → feature/...)
- **Optional reviewers:** US owner + at least 1 from the team roster
- **Minimum to merge:** 1 approval

### Feature PRs (feature/... → develop)
- **Required reviewers:** US owner + Chris Thompson or Cesar Figueroa (at least one)
- **Optional reviewers:** Anton Thorn + any of: Cesar Figueroa, Graham Olson, Jose Olivares, Andrew Scheuer
- **Minimum to merge:** approval obtained

### Bug PRs (bugfix/... → develop)
- **Required reviewers:** Chris Thompson and Cesar Figueroa
- **Optional reviewers:** Anton Thorn
- **Minimum to merge:** approval obtained

### Master PRs (develop → master, production releases)
- **Required reviewers:** April Burns — <email redacted>
- **Minimum to merge:** approval obtained

**Team roster:**

| Name | Email |
|------|-------|
| Chris Thompson | <email redacted> |
| Cesar Figueroa | <email redacted> |
| Graham Olson | <email redacted> |
| Jose Olivares | <email redacted> |
| Andrew Scheuer | <email redacted> |
| Anton Thorn | <email redacted> |

---

## Rules

- PRs are always created as **draft** initially.
- Always link the ADO work item using `AB#` syntax in commit footers.
- For task PRs, link **both** the task (`AB#{task-id}`) and the parent User Story (`AB#{us-id}`) in the ADO PR work items panel.
- PR titles follow conventional commit format: `type(scope): description`.
- Task PRs target the **parent feature branch** (`feature/{us-id}-{slug}`).
- Feature PRs target `develop` after all task PRs are merged.
- Bug fix PRs targeting `develop` must use a `bugfix/*` source branch.
- Never include `.github/session/` or audit files in commits.
- Minimum 1 approval required before merging any PR.
- Every PR description must include AC coverage and the test cases created or updated.
