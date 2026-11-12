---
description: "Record locking (US 11038). Applies to every backend entity/write and every frontend write hook or editable screen."
applyTo: ["src/**/*.ts", "src/**/*.tsx", "**/*.cs"]
---
# Record Locking

- Every EF entity is classified in its configuration (`IsLockableRecord`, `IsPartOfRecord`, `IsPartOfAnyRecord` or `IsNotLockable("reason")`). Unclassified entities are refused at save time with 409 `lock_required`.
- Every frontend write to a lockable record goes through `useLockedMutation`; edit entry points use `LockedEditButton`; new record routes use a mapped param and get sweep fixtures.
- Never add `[SkipRecordLock]`, `ExecuteUpdate`/`ExecuteDelete`/raw SQL on lockable tables, or a raw `useMutation` without an `oxlint-disable-next-line no-restricted-imports -- <reason>`.
- Full rules and troubleshooting: `.github/skills/record-locking/SKILL.md`.
