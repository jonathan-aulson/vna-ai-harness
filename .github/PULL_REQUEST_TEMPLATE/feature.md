<!--
PR Title: feat(scope): short description
Example:  feat(referrals): add eligibility verification endpoint
-->

## Feature Summary

> _What user-facing capability does this PR deliver? Describe the business value._

**User Story:** #<!-- US ID -->  
**Feature branch:** `feature/<!-- branch name -->`

---

## Acceptance Criteria Coverage

_List every AC item this PR addresses. Every row must be checked before requesting review._

| AC # | Description | Verified |
|------|-------------|----------|
| AC1 | | [ ] |
| AC2 | | [ ] |

---

## Changes Made

> _Key technical changes by layer. Be specific — what was added, modified, or removed?_

**Backend:**
-

**Frontend:**
-

**Data / Migrations:**
-

---

## How to Test

> _Step-by-step instructions to verify the feature end-to-end. Include environment setup, test data, and expected outcomes._

**Environment:** <!-- Dev / QA / Local -->

1.
2.
3.

**Expected result:**

---

## Screenshots / Recordings

> _Required for any UI changes. Attach before/after screenshots or a short screen recording._

| Before | After |
|--------|-------|
| | |

---

## Risk & Rollback

**Risk level:** Low / Medium / High

**Areas of risk:** <!-- e.g., schema migration, third-party integration, auth changes -->

**Rollback steps:**

---

## Self-Review Checklist

_All boxes must be checked before marking ready for review._

- [ ] `dotnet build && dotnet test` passes locally
- [ ] `npm run build && npm test` passes locally
- [ ] No console errors or warnings introduced
- [ ] No commented-out code or debug artifacts left in
- [ ] Secrets, credentials, and PII are not in code or logs
- [ ] All AC items in the table above are checked
- [ ] Database migrations (if any) are idempotent and have been reviewed
- [ ] API contract changes are reflected in the NSwag-generated client (`nswag.json`)
- [ ] New routes added to `router.tsx` and protected appropriately
- [ ] New entities are classified for record locking; new writes use `useLockedMutation` (multi-save editors use `editSession`); new record pages have sweep fixtures
- [ ] Figma mockup match verified (if UI work)

---

## Reviewers

**Required:** US owner + at least one of the following as a required reviewer:
- Chris Thompson
- Cesar Figueroa

**Optional:** Anton Thorn + any of the following:
- Anton Thorn
- Graham Olson
- Jose Olivares
- Andrew Scheuer

---

## Reviewer Notes

> _Highlight areas needing focused review, known limitations, or deferred work._

---

<!-- Keep feature PRs scoped to one User Story. Minimum 1 approval required to merge. -->
