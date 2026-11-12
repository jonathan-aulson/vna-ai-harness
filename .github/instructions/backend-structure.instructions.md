---
description: "Use when writing backend code with AI assistance. Feature-based service layer with shared infrastructure and database. Optimized for Copilot navigation and code generation."
applyTo: "**/*.cs"
---

# Backend Architecture: AI-Optimized Feature-Based Service Layer

> **Structural constants** (paths, namespaces, DbContext name, module layout) are
> declared authoritatively in `.github/context/backend-architecture.md`. On any
> conflict, that file wins. This document describes patterns and rules.

## Platform

- .NET 10 with ASP.NET Core Controllers
- EF Core (code-first, single shared DbContext)
- FluentValidation for request/input validation
- **No MediatR, No CQRS** — feature-based service layer
- **Shared infrastructure** — one database, one set of external service adapters

## Philosophy

**Structure organized by feature, infrastructure shared across features.** This is the sweet spot for most projects:
- ✅ Features are self-contained and easy to navigate
- ✅ No infrastructure duplication
- ✅ Single source of truth for database
- ✅ Clear patterns for AI code generation
- ✅ Module files make dependency wiring explicit

## Project Structure

```
backend/src/
  ApiHost/
    ApiHost.csproj
    Program.cs
    appsettings.json
    appsettings.Development.json
    Features/
      {Feature}/                    # Controllers, services, validators, DTOs, repositories
        Dtos/
  Infrastructure/                   # Shared persistence and external services
    Infrastructure.csproj
    Database/
      ApplicationDbContext.cs       # Declares TenantDbContext
      CatalogDbContext.cs           # Approved tenant-catalog context exception
      Models/
      Configurations/
    Migrations/                      # Shared tenant migrations
    Shared/
    ExternalServices/

    Extensions/
      ServiceCollectionExtensions.cs
```

## Feature Structure (Per Entity/Aggregate)

Each feature folder contains everything needed for that feature:

```
Features/Users/
├── UsersController.cs          # REST endpoints
├── UserService.cs              # All User operations (Create, Get, Update, Delete)
├── UserValidator.cs            # Validation for all User DTOs
├── UserExceptions.cs           # Exceptions specific to User feature
├── Dtos/
│   ├── CreateUserRequest.cs
│   ├── GetUserResponse.cs
│   └── UpdateUserRequest.cs
├── UsersModule.cs              # DI wiring (CRITICAL)
└── README.md                   # Feature documentation
```

### What Goes in Each File

- **[Entity]Controller.cs** — REST endpoints. Maps `Result<T>` to HTTP responses (200, 201, 400, 404, 409). Uses policy-based `[Authorize]`. Never contains business logic.
- **[Entity]Service.cs** — All business operations (`CreateAsync`, `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`). Validates via validator, persists via repository, dispatches domain events. Returns `Result<T>`.
- **[Entity]Validator.cs** — FluentValidation rules per operation (`ValidateCreateAsync`, `ValidateUpdateAsync`). One validator class per feature, separate methods per DTO.
- **[Entity]Exceptions.cs** — Feature-specific exceptions grouped in one static class. Extend `ApiException`. Only for unexpected errors — validation failures use `Result<T>`.
- **[Entity]Module.cs** — DI extension method (`AddUsersModule`). Registers service, repository, validator, and event handlers. Called once from `Program.cs`.
- **README.md** — Documents feature overview, file responsibilities, operations, key rules, dependencies, and service flow. Required for every feature.

## Shared Infrastructure Layer

### Database (Single Shared DbContext)

`backend/src/Infrastructure/Database/ApplicationDbContext.cs` declares `TenantDbContext`, the shared `DbContext` with `DbSet<T>` per entity. Its `OnModelCreating` discovers EF configurations from the shared Infrastructure assembly.

`backend/src/Infrastructure/Database/Models/` — plain EF model classes. No domain logic. Navigation properties allowed for EF relationships.

`backend/src/Infrastructure/Database/Configurations/` — one `IEntityTypeConfiguration<T>` per entity. Fluent API only — no data annotations. Defines keys, constraints, indexes, and relationships.

Tenant EF migrations are stored in `backend/src/Infrastructure/Migrations/`.

### External Services (Shared Adapters)

`backend/src/Infrastructure/ExternalServices/` — one folder per external dependency (Email, Payment, Notification). Each contains an interface (`IEmailService`) and a concrete adapter (`SendgridEmailAdapter`). All adapters registered in `Program.cs` and injected into feature services via interface.

## Module Registration in Program.cs

- Register `ApplicationDbContext` with the shared connection string.
- Register shared services (`IEmailService`, `IEventDispatcher`) once.
- Call each feature module extension: `builder.Services.AddUsersModule().AddOrdersModule()...`
- Register middleware (`ErrorHandlingMiddleware`) before `MapControllers`.

## Shared Repository Pattern

Each feature has its own repository in the feature folder that injects the shared `ApplicationDbContext`. Standard methods: `GetByIdAsync`, `SaveAsync`, `UpdateAsync`, `DeleteAsync`. Always use `AsNoTracking()` for read operations. Repository is registered in the feature's module class.

## Key Principles for AI Code Generation

### 1. Consistent File Naming

| Pattern | Examples |
|---------|---------|
| `[Entity]Controller.cs` | `UsersController`, `OrdersController` |
| `[Entity]Service.cs` | `UserService`, `OrderService` |
| `[Entity]Validator.cs` | `UserValidator`, `OrderValidator` |
| `[Entity]Repository.cs` | `UserRepository`, `OrderRepository` |
| `[Entity]Exceptions.cs` | `UserExceptions`, `OrderExceptions` |
| `[Entity]Module.cs` | `UsersModule`, `OrdersModule` |

### 2. Consistent Service Methods

- `CreateAsync(TRequest request) → Result<TResponse>`
- `GetByIdAsync(Guid id) → Result<TResponse>`
- `UpdateAsync(Guid id, TRequest request) → Result<TResponse>`
- `DeleteAsync(Guid id) → Result<bool>`

### 3. Module Pattern (Always the Same)

Every feature module class follows the same structure: a static extension method `Add[Entity]Module` that registers service, repository, validator, and event handlers as `Scoped` dependencies.

## When to Create a New Feature

Create a new feature folder when:
- ✅ You have a new entity/aggregate (Users, Orders, Products, etc.)
- ✅ It has 3+ operations (Create, Get, Update, Delete)
- ✅ It has specific validation rules
- ✅ It might have its own event handlers

Don't create a new feature when:
- ❌ It's just a single operation
- ❌ It's a cross-cutting concern (use Shared instead)
- ❌ It's just a view/query of existing data (put in existing feature)

## Prompting Copilot Effectively

### Bad Prompt
> "Create a new OrderService"

### Good Prompt
> "Create a new Orders feature following the Users feature pattern. Include: OrdersController, OrderService (CreateAsync, GetByIdAsync, UpdateStatusAsync), OrderValidator, OrderExceptions, Dtos folder, OrdersModule, README.md. Reference Features/Users/ as the template."

### Best Prompt
> "Look at Features/Users/ folder structure and UsersModule.cs. Create Features/Orders/ with the same pattern: OrdersController (POST /api/orders, GET /api/orders/{id}, PATCH /api/orders/{id}/status), OrderService (CreateAsync, GetByIdAsync, UpdateStatusAsync, DeleteAsync), OrderValidator, OrderExceptions (OrderNotFound, InvalidStatus, DuplicateOrder), Dtos/, OrdersModule. Add `builder.Services.AddOrdersModule()` to Program.cs."

## API Response Format

All endpoints return consistent format via `ErrorHandlingMiddleware`:

- **Success (200, 201)**: response object directly
- **Validation failure (400)**: `ProblemDetails` with `title` and `detail`
- **Not found (404)**: `ProblemDetails`
- **Conflict (409)**: `ProblemDetails`

## Summary

✅ **Feature-based organization** — Find all code for Users in Features/Users/  
✅ **Shared infrastructure** — One database, no duplication  
✅ **Clear patterns** — AI can predict exactly what to create  
✅ **Module files** — Explicit DI registration, easy to understand dependencies  
✅ **Scalability** — Add features without changing infrastructure  
✅ **Maintainability** — Every feature follows same structure  
✅ **AI-friendly** — Consistent naming, predictable patterns  

One feature folder per entity/aggregate. One module file to register all dependencies. One shared database layer across all features.
