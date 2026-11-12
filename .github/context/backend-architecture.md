# Backend Architecture — Source of Truth

**This document is the single authoritative declaration of backend structural constants.**
The repository code is reality; this file declares that reality in one place. Everything
else defers to it:

- `backend-codegen-agent` **copies** the Codegen Constants block below into every
  `BackendGenerationContract.json` — it never derives or guesses these values.
- `.github/instructions/backend-structure.instructions.md` describes patterns and
  rules (the *why* and *how*); for structural constants it defers to this file.
- Reviewers treat a generated contract that contradicts this file as a finding
  against the contract, and code that contradicts this file as architecture drift
  to be escalated — not silently accommodated.

**On mismatch, stop — never guess.** If an agent's drift check (below) finds the repo
does not match this file, the agent must stop and report the mismatch to the human.
Either the code drifted (fix the code) or the architecture changed (update this file,
in the same commit as the code change).

**Changing the architecture?** One workflow: change the code, update this file in the
same commit, then check `backend-codegen-agent.agent.md` and
`backend-structure.instructions.md` for any prose that restates what changed.

Last verified against the repo: 2026-10-15 (US 11616 integration).

## The Layout, in Plain Terms

One solution and one shared tenant database context. HTTP features live under
`backend/src/ApiHost/Features/{Feature}/`; persistence models, Fluent API configurations,
and the `TenantDbContext` implementation live in the shared
`backend/src/Infrastructure/` project. The context class is declared in
`Database/ApplicationDbContext.cs` and discovers configurations from the Infrastructure
assembly. Tenant migrations are kept in `backend/src/Infrastructure/Migrations/`.
`CatalogDbContext` in `Database/CatalogDbContext.cs` is the approved separate context
for tenant catalog metadata. This repository does not use per-feature Application /
Domain / Infrastructure projects or `SharedKernel`/`DatabaseMigrations` directories.

## Codegen Constants (copied verbatim into generation contracts)

`{Feature}` is the feature folder name in PascalCase (e.g., `GeneralLedger`). Everything
else is literal.

```json
{
  "project": {
    "backendRoot": "backend",
    "targetFramework": "net10.0",
    "apiProjectPath": "backend/src/ApiHost/ApiHost.csproj",
    "infrastructureProjectPath": "backend/src/Infrastructure/Infrastructure.csproj",
    "testProjectPath": "backend/tests/Backend.Tests/Backend.Tests.csproj",
    "namespaceRoot": "ApiHost.Features.{Feature}",
    "resultNamespace": "Infrastructure.Shared",
    "applicationNamespace": "ApiHost.Features.{Feature}",
    "repositoryNamespace": "ApiHost.Features.{Feature}",
    "domainNamespace": "Infrastructure.Database.Models",
    "infrastructureNamespace": "Infrastructure.Database",
    "applicationRoot": "backend/src/ApiHost/Features/{Feature}",
    "domainRoot": "backend/src/Infrastructure/Database/Models",
    "infrastructureRoot": "backend/src/Infrastructure",
    "configurationRoot": "backend/src/Infrastructure/Database/Configurations",
    "testRoot": "backend/tests/Backend.Tests",
    "testNamespace": "Backend.Tests"
  },
  "architecture": {
    "pattern": "Controller-Service-Validator-Repository-Feature",
    "dbContext": "TenantDbContext",
    "dbContextPath": "backend/src/Infrastructure/Database/ApplicationDbContext.cs",
    "catalogDbContextPath": "backend/src/Infrastructure/Database/CatalogDbContext.cs",
    "migrationsRoot": "backend/src/Infrastructure/Migrations",
    "resultType": "Result<T>",
    "useAsyncOnly": true,
    "useAsNoTrackingForReads": true,
    "useFluentValidation": true,
    "avoidPiiLogging": true
  }
}
```

## Conventions (per-task values follow these rules)

| Contract field | Rule | Example |
|---|---|---|
| `feature.routeBase` | `api/` + plural, lowercase | `api/clients` |
| `dataModel.schema` | module name, lowercase; never a global default schema | `client` |
| `dataModel.tableName` | plural PascalCase | `Clients` |
| `feature.primaryKey` | from the entity's `EntityBase<T>` generic; `Guid` for new entities unless the story says otherwise | `{ "name": "Id", "type": "Guid" }` |

## Conformance Tests (agents create and maintain these as the project is built)

The architecture is enforced by the build, not by review discipline. The backend test
project must contain `Backend.Tests/Architecture/ArchitectureConformanceTests.cs`
implementing the assertions below (NetArchTest.Rules or plain reflection — no other
dependencies). **Bootstrap rule: the agent implementing the first BE or DATA slice of
the project creates this file; every later slice keeps it passing.** These tests run
in the standard `dotnet test` quality gate (P7c), so violations fail the slice.

Required assertions (keep test names as listed):

| Test | Asserts |
|---|---|
| `SingleSharedDbContext` | Exactly one tenant-data `DbContext` subclass named `TenantDbContext`; approved exception: `CatalogDbContext` for tenant catalog metadata |
| `NoFeatureLocalDbContext` | No feature-local `DbContext` is declared under `ApiHost/Features`; approved exception remains only `backend/src/Infrastructure/Database/CatalogDbContext.cs` |
| `ApiHostReferencesSharedInfrastructure` | ApiHost feature code references the shared Infrastructure project |
| `InfrastructureDoesNotReferenceApiHost` | The shared Infrastructure project does not reference ApiHost |
| `RepositoryInterfacesLiveInFeature` | Feature repository interfaces live in their `ApiHost.Features.*` namespace |
| `NoGlobalDefaultSchema` | `TenantDbContext` does not call `HasDefaultSchema`; schemas, when required, are configured explicitly on entities |
| `SourceOfTruthMatchesCode` | Parses the Codegen Constants JSON block and checks the declared ApiHost feature root, shared Infrastructure model/configuration/migration roots, tenant context, catalog exception, project files, and namespaces against the repository |

`SourceOfTruthMatchesCode` is the drift guard: changing the code without updating this
document — or this document without the code — turns the build red. When architecture
changes are intentional, update code, this document, and the affected assertions in the
same commit.

## Drift Check (mandatory before generation)

Before running the Scriban CLI, `backend-codegen-agent` verifies in ONE batched turn:

1. `backend/src/Infrastructure/Database/ApplicationDbContext.cs` exists and declares
    class `TenantDbContext`.
2. `backend/src/Infrastructure/Database/CatalogDbContext.cs` is the only approved
    non-tenant DbContext and declares class `CatalogDbContext`.
3. The target feature exists under `backend/src/ApiHost/Features/{Feature}` (or the
    task explicitly creates a new feature).
4. No feature-local `*DbContext.cs` exists under `backend/src/ApiHost/Features/**`.

Any check fails → **stop, report drift to the orchestrator, do not generate.**

> TODO (nice-to-have): move this check into the CLI as
> `BackendCodegen.Cli validate --contract <path>` so it is enforced deterministically
> at every generation regardless of which agent invokes it.
