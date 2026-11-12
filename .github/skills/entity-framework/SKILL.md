---
name: entity-framework
description: 'EF Core conventions. Fluent API configuration, async queries only, single shared ApplicationDbContext, no lazy loading, no data annotations.'
---

# Entity Framework Skill

## Core Conventions

- **Fluent API only** — no data annotations for configuration.
- **Async queries only** — never use synchronous methods.
- **Single shared `ApplicationDbContext`** — one DbContext for all features; accessed only through each feature's own repository.
- **No lazy loading** — always use explicit `.Include()` or projections.
- **`AsNoTracking()` for reads** — always on read-only queries to avoid change-tracking overhead.

## Configuration Pattern

One `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Database/Configurations/`.
Fluent API only — defines keys, constraints, indexes, and relationships.

## DbContext Pattern

`Infrastructure/Database/ApplicationDbContext.cs` — one `DbContext` with `DbSet<T>` per entity model.
`OnModelCreating` applies all configurations via `modelBuilder.ApplyConfigurationsFromAssembly()`.

## Query Patterns

- Always async: `FirstOrDefaultAsync`, `ToListAsync`, `AnyAsync` with `CancellationToken`.
- Use `AsNoTracking()` on all reads.
- Use `.Select()` projections for read-heavy paths — avoid loading full entities when only a subset of fields is needed.
- Use explicit `.Include()` only when navigation properties are needed.

## Owned Entities (Value Objects)

Value objects are configured with `OwnsOne` in the entity configuration. No separate `DbSet<T>` needed.
