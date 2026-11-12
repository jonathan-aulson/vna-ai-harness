---
name: branching-strategy
description: 'Branch naming conventions and lifecycle rules. feature/{id}-{slug} for user stories (implemented whole in one pass), bugfix/{id}-{slug} for bug fixes. How to slug-ify titles, when to create each type.'
---

# Branching Strategy Skill

## Branch Types

| Type | Pattern | Created from | Notes |
|------|---------|-------------|-------|
| Feature | `feature/{id}-{slug}` | `develop` | New features and User Stories — the whole story is implemented on this one branch |
| Bug fix | `bugfix/{id}-{slug}` | `develop` | Required prefix for bug fixes targeting develop |

## Slug Rules

1. Lowercase the title.
2. Replace spaces and special characters with hyphens.
3. Remove consecutive hyphens.
4. Trim to max 50 characters.
5. Remove trailing hyphens.

Examples:
- "Add Patient Referral Form" → `add-patient-referral-form`
- "Fix #123: SSN Validation Bug" → `fix-123-ssn-validation-bug`

## Lifecycle

### Feature Branches
```bash
# Create from develop, bootstrapping develop from main when needed
git fetch origin

if git show-ref --verify --quiet refs/heads/develop; then
	git checkout develop
	git pull --ff-only origin develop
elif git ls-remote --exit-code --heads origin develop >/dev/null 2>&1; then
	git checkout -b develop origin/develop
else
	git checkout main
	git pull --ff-only origin main
	git checkout -b develop
fi

git checkout -b feature/1234-add-patient-referral-form
```

### Bugfix Branches
```bash
# Create from develop for bug fixes targeting develop
git fetch origin
git checkout develop
git pull --ff-only origin develop
git checkout -b bugfix/1234-fix-login-timeout
```

## Rules

- Feature branches ALWAYS branch off develop. If develop does not exist, create it from main first.
- Bugfix branches ALWAYS branch off develop and must use the `bugfix/*` prefix (required by CI for develop merges).
- Before creating a feature branch, sync develop with origin.
- A User Story's entire implementation happens on its single feature branch — no child task branches.
- Feature branches merge back to develop via PR.
- Delete branches after merge.
- Never force-push to shared branches.
