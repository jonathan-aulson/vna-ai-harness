---
name: ddd-feature-slices
description: 'The canonical structure every backend feature must follow: Controller → Service → Repository → Validator all in one feature folder. No MediatR, no CQRS — feature-based service layer with shared ApplicationDbContext.'
---

# Feature-Based Service Layer

## Feature Structure

Every backend feature is organized in a self-contained folder:

```
Features/{FeatureName}/
├── {FeatureName}Controller.cs    # HTTP endpoints (maps Result<T> to IActionResult)
├── {FeatureName}Service.cs        # Business logic orchestration
├── {FeatureName}Repository.cs     # Data access via shared ApplicationDbContext
├── {FeatureName}Validator.cs      # FluentValidation rules per operation
├── {FeatureName}Exceptions.cs     # Feature-specific exceptions (extend ApiException)
├── Dtos/
│   ├── {FeatureName}Request.cs    # Input DTO
│   └── {FeatureName}Response.cs   # Output DTO
├── {FeatureName}Module.cs        # DI registration (Add{Feature}Module extension)
└── README.md                      # Feature documentation
```

## Feature Boundary Rules

- Features must NOT call another feature's service directly.
- Cross-feature communication goes through domain events dispatched via `IEventDispatcher`.
- The shared `ApplicationDbContext` is injected into each feature's **repository only** — never into controllers or services.
- External adapters (email, SMS, payment) live in `Infrastructure/ExternalServices/` and are injected into services via interface.

## Controller Pattern

- Maps HTTP verbs to service method calls.
- Maps `Result<T>` to `IActionResult` (200/201/400/404/409).
- Uses policy-based `[Authorize(Policy = ...)]`.
- Never contains business logic or direct DB access.

## Service Pattern

Each service handles all operations for a feature and returns `Result<T>`:

- `CreateAsync(TRequest) → Result<TResponse>`
- `GetByIdAsync(Guid id) → Result<TResponse>`
- `UpdateAsync(Guid id, TRequest) → Result<TResponse>`
- `DeleteAsync(Guid id) → Result<bool>`

Service flow: validate → orchestrate domain logic → persist via repository → dispatch events → map to response DTO.

## Repository Pattern

- Injects shared `ApplicationDbContext`.
- Standard methods: `GetByIdAsync`, `SaveAsync`, `UpdateAsync`, `DeleteAsync`.
- Always `AsNoTracking()` for read operations.
- Registered as `Scoped` in the feature's module class.

## Validator Pattern

One validator class per feature with separate methods per operation:

- `ValidateCreateAsync(TRequest) → ValidationResult`
- `ValidateUpdateAsync(TRequest) → ValidationResult`

Validation failures return `Result<T>.Failure(...)` — never throw exceptions for expected validation errors.

## Module Registration Pattern

Each feature has one static extension method registered in `Program.cs`:

```
Add{Feature}Module(this IServiceCollection services)
```

Registers service, repository, validator, and event handlers as `Scoped` dependencies.
