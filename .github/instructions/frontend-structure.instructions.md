---
description: "Use when writing React frontend code, creating feature modules, managing state, or adding routes. Covers feature-based architecture, React Query, NSwag client usage, and routing patterns."
applyTo: ["src/**/*.ts", "src/**/*.tsx"]
---
# Frontend Architecture

## Stack

- React 19 + TypeScript
- Vite (bundler)
- React Query (server state)
- React Router v7 (routing)
- NSwag (generated API client)

## Feature-Based Folder Structure

```
src/
  features/
    referrals/
      api/          # React Query hooks wrapping NSwag client calls
      components/   # UI components specific to this feature
      hooks/        # Custom hooks for this feature
      pages/        # Route page components
      types/        # Feature-specific TypeScript interfaces
    clinical/
    scheduling/
    ...
  shared/
    components/     # Reusable UI components
    hooks/          # Shared hooks
    utils/          # Utility functions
    types/          # Shared TypeScript types
```

## API Client

- Generate TypeScript API client using **NSwag** from Swagger/OpenAPI spec
- NEVER hand-write API clients unless explicitly needed
- Generated client code must be isolated from handwritten frontend code
- Use the generated types directly — do NOT duplicate DTOs manually

## State Management

- **Server state**: React Query only — do NOT duplicate in global stores
- **UI state**: Local component state (`useState`, `useReducer`)
- **No Redux** by default — no global state library unless explicitly justified
- React Query handles caching, invalidation, background refetching

## Routing

- React Router v7 with lazy-loaded route components
- Protected routes use Azure Entra auth guard
- Route-level data loading coordinated with React Query

## Type Safety

- Prefer generated DTO/client types from NSwag over hand-written interfaces
- Explicit TypeScript interfaces on ALL component props
- No `any` — use `unknown` if type is truly unknown, then narrow
