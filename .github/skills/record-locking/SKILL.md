---
name: record-locking
description: 'Single-editor record locking (US 11038). Use whenever a change adds an entity, a write endpoint, a write hook or an editable screen, or when a save fails with 409 lock_required / unclassified. Covers EF classification, SaveChanges enforcement, useLockedMutation, LockedEditButton, RecordScope, edit sessions and sweep fixtures.'
---

# Record Locking Skill

Only one user at a time may edit a record. The backend enforces this on every save; the frontend
acquires, renews and releases the lock. **Writes fail closed:** anything not handled is refused with
409 `lock_required`, so a new feature that skips these rules breaks loudly instead of silently.

## How it works

- Every EF entity is **classified** in its configuration. `RecordGraph` resolves any entity to the
  root record(s) it belongs to.
- `TenantDbContext.SaveChangesAsync` (`backend/src/Infrastructure/Database/ApplicationDbContext.cs`)
  refuses a write unless the current user holds a live lock on each affected root. Roots created in
  the same request are allowed. Synchronous `SaveChanges` on lockable entities is refused.
- Locks live in `dbo.record_lock`: lease 300 s, heartbeat 30 s, owned per user (tenant + oid).
- API (`RecordLocksController`): `POST|PUT …/heartbeat|DELETE|GET api/locks/{type}/{id}` and
  `GET api/locks/{type}?ids=` (batch, max 100).
- Roots today: `Client`, `Payer`, `CongregateSite`, `EmergencyContact`, `ReferralSource`.

## Backend: a new entity

1. **Classify it** with exactly one call at the top of `Configure` (Fluent API, no annotations):

   | Question | Classification |
   |---|---|
   | Does a user edit it on its own screen? | `builder.IsLockableRecord();` |
   | Is it edited as part of another record's screen? | `builder.IsPartOfRecord<TEntity, TParent>(x => x.ParentId);` |
   | …and the FK may be null for rows that belong to no record? | `IsPartOfRecord<…>(x => x.ParentId, OwnerlessIs.NotLockable)` |
   | Does it link two records (household A/B)? | `builder.IsPartOfAnyRecord<TEntity, TParent>(x => x.AId, x => x.BId);` |
   | Settings, lookups, billing or system data? | `builder.IsNotLockable("billing: why");` (non-empty reason, `area: reason` style) |

   Examples: `CongregateSiteConfiguration` (record), `AuthorizationConfiguration` (part of `Client`),
   `ClientAuthorizationMealConfiguration` (part of a part: resolves to `Client`),
   `DocumentLibraryScopeConfiguration` (ownerless rows not lockable), `ClaimConfiguration` (not lockable).
2. **A new root** gets a rowversion and a migration:
   ```csharp
   public byte[] Version { get; set; } = Array.Empty<byte>();            // model
   builder.Property(x => x.Version).HasColumnName("version").IsRowVersion(); // configuration
   ```
   Then add the type to the frontend `LockableRecordType` union (`features/record-locks/contracts`).
3. **Never bypass `SaveChanges`** on lockable tables: no `ExecuteUpdate`, `ExecuteDelete`,
   `ExecuteSql*` or `FromSql` writes. The architecture test (`RecordLockArchitectureTests`) holds the
   allow-list: statement billing, questionnaire templates and the record-lock infrastructure itself.
4. **Never add `[SkipRecordLock]`.** It is allowed only on the BOP integration controllers and
   `RecordLocksController`; a new one needs team approval and an allow-list change.
5. Run the record-lock tests: `RecordLockArchitectureTests` fails on any unclassified entity.

Backend codegen does step 1 for you: every entity in `BackendGenerationContract.json` needs a
`recordLock` object (see `tools/BackendCodegen.Cli/README.md`).

## Frontend: a new write or editable screen

All lock code is in `frontend/src/features/record-locks`.

1. **Every write hook uses `useLockedMutation`**, never `useMutation` (oxlint `no-restricted-imports`
   enforces this).
   ```ts
   return useLockedMutation({
     record: (request: UpdateClientQuickEditRequest) => ({ type: 'Client', id: request.clientId }),
     mutationFn: (request) => apiClient.clientsPUT(request.clientId, payload),
   });
   ```
   - Omit `record` when the component is inside a `RecordScope` (route or explicit).
   - Creates use `createRecord: (data) => ({ type, id: data.newId })` to lock the new record.
   - `hold: true` only for create flows that must keep the new record locked (the client wizard).
   - Not a lockable write (read-only POST, billing, settings)? Keep `useMutation` with
     `// oxlint-disable-next-line no-restricted-imports -- <specific reason>` on the import.
2. **Lifecycle.** By default a save is one-shot: acquire if needed → save → release on success. A
   failed save keeps the lock so the user can retry. Overlapping saves release once, after the last.
3. **Edit sessions.** An editor that saves several times before the user is done (the client wizard,
   `PetInformationQuestionnaireForm`) registers with
   `useRecordLock(type, id, { editSession: true })`. Saves inside it keep the lock; the editor releases
   on finish/cancel/leave (unmount and `pagehide` also release). **Do not** use `editSession` on a
   single-save modal or panel: its lock would stay held after the save.
4. **Edit entry points use `LockedEditButton`** (acquires before `onEdit`; disabled while `Denied` or
   `Acquiring`; stays enabled on `Lost`/`Expired` so clicking re-acquires).
5. **Read-only gating.** Viewing surfaces (lists, drawers, detail panels) take **no lock on open** and
   disable inputs with `!lock.canEdit`. Edit-mode surfaces (wizard, edit page, edit modal) use
   `lock.status !== 'Editing'`.
6. **Scope.** Record routes use a param from `recordRouteParams.ts` (`clientId`, `siteId`, …) or add
   one; `RecordScope` then resolves automatically. Anywhere else wrap the editor in
   `<RecordScope type="…" id={…}>`.
7. **Sweep fixtures.** Add every new record page or edit surface to `lockSweepSurfaces.ts`. The sweep
   test fails on an edit control outside `LockedEditButton`/`useLockedMutation` or an unmapped write.
   Mark purely navigational buttons inside a gated area with `data-lock-exempt`.
8. Nothing else is per-feature: `RecordLockBanner`, `RecordLockIndicator` (lists use
   `useRecordLocksBatch`), heartbeat, recovery, the global 409 handler and session-timeout release are
   shared.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| 409 `lock_required`, `reason: "unclassified"` | The entity has no classification call. Add one (backend step 1). |
| 409 `lock_required` on your own save | The write isn't `useLockedMutation`, the scope/`record` resolver points at the wrong record, or the route param isn't mapped. |
| Editor turns read-only after its first save | It's a multi-save editor without `editSession: true`. |
| Lock still held (heartbeats continue) after a modal saved | `editSession` used on a single-save editor, or a manual `acquire()` with no release. |
| "You're editing this in another tab" | The same user holds the lock in another tab; expected. |
| Architecture test fails on raw SQL / `[SkipRecordLock]` | Use `SaveChanges`; don't extend the allow-list without team approval. |

## Known limits

Two tabs of the same user are not protected from each other; viewers refresh on a 20 s poll; settings,
lookups, billing and bulk operations are not locked; there is no supervisor take-over.
