---
name: ef-migration
description: 'EF Core migration workflow. Use descriptive migration names only (EF adds timestamp), shared migrations project usage, module-scoped context flag, never editing existing migrations, rollback strategy, Add-Migration and Update-Database commands per module.'
---

# EF Migration Skill

## Naming Pattern

```
DescriptiveName
```

EF Core automatically prefixes migration files with a timestamp.
Do not include a timestamp in the name argument.

Filename guard:
- Required: exactly one leading 14-digit timestamp, then underscore, then descriptive name.
- Accept: `20260820173000_AddPatientReferralTable.cs`
- Reject: `20260820162407_20260820173000_AddPatientReferralTable.cs`

Examples:
- `AddPatientReferralTable`
- `AddStatusColumnToReferrals`
- `CreatePaymentIntentIndex`

## Shared Migrations Project

All EF Core migrations live in the shared project:

- `backend/src/DatabaseMigrations/DatabaseMigrations.csproj`

Use a module-specific output folder inside that project so each DbContext keeps its own snapshot file:

- Referrals: `Migrations`
- Financial: `Migrations/Financial`

## Commands

### Add Migration

All migrations use the single shared `ApplicationDbContext`:

```bash
dotnet ef migrations add AddPatientReferralTable \
  --context ApplicationDbContext \
  --project backend/src/DatabaseMigrations/DatabaseMigrations.csproj \
  --startup-project backend/src/ApiHost/ApiHost.csproj \
  --output-dir Migrations
```

### Update Database

```bash
dotnet ef database update \
  --context ApplicationDbContext \
  --project backend/src/DatabaseMigrations/DatabaseMigrations.csproj \
  --startup-project backend/src/ApiHost/ApiHost.csproj
```

### Rollback

```bash
dotnet ef database update {PreviousMigrationName} \
  --context ApplicationDbContext \
  --project backend/src/DatabaseMigrations/DatabaseMigrations.csproj \
  --startup-project backend/src/ApiHost/ApiHost.csproj
```

## Rules

1. **Never edit existing migrations** — always create a new one.
2. **Always use `ApplicationDbContext`** — there is one shared context; always specify `--context ApplicationDbContext`.
3. **Shared migrations project** — always target `backend/src/DatabaseMigrations/DatabaseMigrations.csproj`.
4. **All migrations in one output dir** — use `--output-dir Migrations`; no per-feature sub-folders needed with a single context.
5. **One concern per migration** — don't mix table creation with data seeding.
6. **Always have a rollback strategy** — test `Down()` method works.
7. **Check pending migrations** before adding new ones:
   ```bash
   dotnet ef migrations list --context {ModuleDbContext} --project backend/src/DatabaseMigrations/DatabaseMigrations.csproj --startup-project backend/src/ApiHost/ApiHost.csproj
   ```
8. **Verify filename format after generation** — migration file must match `^\d{14}_[A-Za-z].*` and must not match `^\d{14}_\d{14}_.*`.

## Migration Code Review Checklist

- [ ] Down() method properly reverses Up()
- [ ] No data loss in column type changes
- [ ] Indexes added for frequently queried columns
- [ ] Foreign keys have appropriate cascade behavior
- [ ] Large table alterations use batched approach
