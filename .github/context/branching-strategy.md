# VNA Project: Branching Strategy & Pull Request Guidelines

**Last Updated:** July 20, 2026
**Project:** VNA — MOW 2.0
**Purpose:** Maintain a structured and efficient development process through consistent branch naming, PR workflows, and code organization.

---

## Overview

This document outlines the branching strategy and pull request (PR) procedures for the VNA project. All team members must follow these guidelines to ensure consistency, facilitate collaboration, and maintain an organized development workflow.

**Applicable codebase:** `mow2` repository

---

## Development Workflow

### 1. Create a Story Branch

Initiate a new feature branch from `develop` using the standardized naming convention.

**Pattern:** `feature/{story-id}-{description}`

**Examples:**
```
feature/142-user-authentication
feature/205-payment-processing
feature/89-report-dashboard
```

### 2. Developers Create Task Branches

Each developer working on a task within the story creates a sub-branch from the story branch.

**Pattern:** `task/{task-id}-{description}`

**Examples:**
```
task/142-login-form-component
task/142-authentication-service
task/205-payment-gateway-integration
```

**Naming guidelines:**
- Keep descriptions concise but descriptive
- Use hyphens to separate words — no underscores
- Do not include area prefix (fe/be/data) in branch names

### 3. Bug Fix Branches

For bug fixes targeting `develop`, use the `bugfix/*` prefix. This is enforced by CI — branches not following this convention will fail the build.

**Pattern:** `bugfix/{id}-{description}`

**Examples:**
```
bugfix/156-login-timeout-issue
bugfix/203-route-assignment-error
```

---

## Merging: Task → Story Branch

When a developer completes their task:

1. Push the task branch to the remote repository
2. Create a pull request targeting the **story branch** (not `develop`)
3. Add reviewers (see below)
4. Address feedback and ensure all checks pass
5. Merge upon obtaining at least **1 approval** — use **squash and merge**

**Commit message format:** `Task {task-id}: {description}`

### Reviewers (Task PRs)

| Role | Reviewers |
|------|-----------|
| **Optional** | US owner + at least 1 from the team roster below |

**Team roster:**

| Name | Email |
|------|-------|
| Chris Thompson | <email redacted> |
| Cesar Figueroa | <email redacted> |
| Graham Olson | <email redacted> |
| Jose Olivares | <email redacted> |
| Andrew Scheuer | <email redacted> |
| Anton Thorn | <email redacted> |

### PR Checklist (Task PRs)

- [ ] Code follows project style guidelines
- [ ] All tests pass (unit, integration)
- [ ] Changes are documented (comments, README updates)
- [ ] No breaking changes to existing functionality
- [ ] Minimum 1 approval obtained

---

## Merging: Story Branch → Develop

After all tasks are completed and validated:

1. Ensure all task branches have been merged into the story branch
2. Run full integration tests on the story branch
3. Create a PR to merge the story branch into `develop`
4. Ensure the story branch name follows `feature/*` naming convention
5. Add reviewers (see below)
6. Merge upon approval — use **squash and merge**

**Commit message format:** `Feature {us-id}: {description} [{feature-branch-name}]`

### Reviewers (Feature PRs)

| Role | Reviewers |
|------|-----------|
| **Required** | US owner + **Chris Thompson** or **Cesar Figueroa** (at least one) |
| **Optional** | Anton Thorn + any of: Cesar Figueroa, Graham Olson, Jose Olivares, Andrew Scheuer |

---

## Merging: Bug Fix → Develop

Bug fix PRs follow the same process as feature PRs but use the `bugfix/*` source branch.

### Reviewers (Bug PRs)

| Role | Reviewers |
|------|-----------|
| **Required** | Chris Thompson + Cesar Figueroa |
| **Optional** | Anton Thorn |

---

## Merging: Develop → Master (Releases)

**Strategy:** Standard merge or rebase (maintain full history)
**Tagging:** Tag with semantic version (e.g., `v1.2.0`)
**Release notes:** Document all features, fixes, and changes

### Reviewers (Master PRs)

| Role | Reviewers |
|------|-----------|
| **Required** | April Burns |

---

## Branch Naming Conventions for Develop Integration

> **Critical:** When merging any branch into `develop`, the source branch name **must** follow one of these conventions. Branches that do not comply will result in a **failed build**.

| Prefix | Use |
|--------|-----|
| `feature/*` | New features and enhancements |
| `bugfix/*` | Bug fixes and patches |
| `backmerge/*` | Backmerges from other branches |

**Valid examples:**
```
✓ feature/142-user-authentication
✓ bugfix/156-login-timeout-issue
✓ backmerge/session-handling
```

**Invalid examples (will fail CI):**
```
✗ task/142-login-form        ← task branches target story branches, not develop
✗ dev/some-feature           ← does not follow convention
✗ fix-login-bug              ← missing prefix
```

---

## Pull Request Best Practices

### PR Title Format

PR titles follow **conventional commit format**: `type(scope): short description`

| Type | Use |
|------|-----|
| `feat` | New feature or user-facing capability |
| `fix` | Bug fix |
| `chore` | Build, CI, configuration, or governance changes |
| `docs` | Documentation only |
| `refactor` | Code restructuring with no behavior change |
| `test` | Test additions or updates |

**Examples:**
```
feat(referrals): add eligibility verification endpoint
fix(scheduling): correct route assignment on depot change
chore(governance): add repository PR templates
```

### PR Description Templates

Three purpose-built templates are available in `.github/PULL_REQUEST_TEMPLATE/`. When creating a PR in Azure DevOps, append `?templateId=<name>` to the PR creation URL:

| Template | URL parameter | Use when |
|----------|--------------|---------|
| `feature.md` | `?templateId=feature` | Story branch → develop |
| `task.md` | `?templateId=task` | Task branch → story branch |
| `bug.md` | `?templateId=bug` | Bug fix → develop |

---

## Code Review Standards

| Standard | Detail |
|----------|--------|
| Minimum approvals | 1 required |
| Preferred (critical features) | 2+ approvals |
| Review scope | Code quality, testing, documentation, adherence to guidelines |
| Turnaround target | Tasks: within 2–3 hours |

---

## PR Merge Requirements

All of the following must be satisfied before merging:

- [ ] All automated checks pass (linting, tests, builds)
- [ ] Required code review approvals obtained
- [ ] No unresolved conversations
- [ ] Source branch follows naming conventions (for `develop` merges)
- [ ] Branch is up to date with target branch
- [ ] Documentation updated as needed

---

## Branch Protection Rules

| Branch | Rule | Detail |
|--------|------|--------|
| `develop` | Require status checks | All CI/CD pipelines must pass |
| `develop` | Require code reviews | Minimum 1 approval |
| `develop` | Dismiss stale reviews | Reviews reset if new commits are pushed |
| `develop` | Require branch to be up to date | No outdated PRs allowed |
| `develop` | Require squash commits | All PRs must squash on merge |
| `master` | Enforce all above rules | Even stricter requirements for production |

---

## Merging Strategy Summary

| Merge direction | Strategy | Commit message format |
|-----------------|----------|-----------------------|
| Task → Story branch | Squash and merge | `Task {task-id}: {description}` |
| Story branch → Develop | Squash and merge | `Feature {us-id}: {description} [{branch}]` |
| Develop → Master | Standard merge or rebase | Tag: `v{major}.{minor}.{patch}` |
