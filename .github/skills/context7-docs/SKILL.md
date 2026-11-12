---
name: context7-docs
description: "Fetch current library/framework documentation via Context7 MCP. Use when planning implementations, resolving API signatures, checking configuration patterns, or validating approach against latest docs. Covers all libraries in the MOW 2.0 stack."
---

# Context7 Documentation Skill

## Purpose

Retrieve up-to-date documentation for any library, framework, or SDK used in implementation — ensuring code references current API signatures rather than stale training data.

## When to Use

- **Planning phase**: Before producing implementation plans, resolve current API patterns for libraries the story touches.
- **Task brief creation**: Embed relevant API references (method signatures, config patterns) directly in task briefs so implementing agents don't need to re-fetch.
- **Implementation**: When encountering unfamiliar or recently-changed APIs.
- **Review**: To verify that implementation follows current best practices.

## When NOT to Use

- General programming concepts (loops, algorithms, design patterns)
- Business logic or domain-specific rules
- Refactoring code that doesn't depend on external library APIs
- Code you've already verified is correct

## Workflow

### Step 1: Resolve Library ID

Always resolve the library ID first. Use the official library name with proper punctuation.

```
Tool: mcp_context7_resolve-library-id
Input:
  libraryName: "EF Core"
  query: "How to configure DbContext with Fluent API"
```

This returns a Context7-compatible library ID (e.g., `/dotnet/efcore`).

### Step 2: Query Documentation

Use the resolved ID to fetch focused documentation.

```
Tool: mcp_context7_query-docs
Input:
  libraryId: "/dotnet/efcore"
  query: "Fluent API entity configuration owned types"
```

### Constraints

- **Max 3 calls per question** — be specific with queries to get what you need efficiently.
- **Resolve before querying** — never guess a library ID; always resolve first (unless the user provides one in `/org/project` format).
- **Be specific** — "How to set up JWT authentication in ASP.NET Core" is better than "auth".

## MOW 2.0 Technology Stack Reference

Use this table to identify which libraries to look up based on the work being done:

### Backend (.NET 10)

| Technology | Lookup Name | Common Queries |
|-----------|-------------|----------------|
| EF Core | `EF Core` | DbContext config, migrations, Fluent API, owned entities |
| ASP.NET Core | `ASP.NET Core` | Minimal APIs, middleware, authentication, authorization |
| FluentValidation | `FluentValidation` | Validator rules, custom validators, async validation |
| MediatR | `MediatR` | Handlers, pipeline behaviors, notifications |
| Polly | `Polly` | Retry policies, circuit breaker, timeout |
| xUnit | `xUnit` | Test patterns, fixtures, theory data |
| NSubstitute | `NSubstitute` | Mocking, returns, received calls |
| TestContainers | `Testcontainers` | SQL Server container, lifecycle, connection strings |
| Stripe .NET | `Stripe .NET` | PaymentIntent, webhooks, idempotency |
| Azure SDK | `Azure SDK for .NET` | Blob Storage, Communication Services, Service Bus |

### Frontend (React 19 + TypeScript)

| Technology | Lookup Name | Common Queries |
|-----------|-------------|----------------|
| React | `React` | Hooks, components, context, suspense |
| React Router | `React Router` | v7 routing, loaders, lazy routes, nested layouts |
| TanStack Query | `TanStack Query` | useQuery, useMutation, cache invalidation |
| Vite | `Vite` | Config, plugins, proxy, env variables |
| Vitest | `Vitest` | Testing, mocking, coverage |
| React Testing Library | `React Testing Library` | Queries, user events, waitFor, accessibility |
| Tailwind CSS | `Tailwind CSS` | Utility classes, config, responsive design |
| NSwag | `NSwag` | Client generation, configuration |

### Infrastructure

| Technology | Lookup Name | Common Queries |
|-----------|-------------|----------------|
| Azure SQL | `Azure SQL` | Connection, scaling, security |
| Redis | `Redis` | Caching patterns, pub/sub |
| Azure Service Bus | `Azure Service Bus` | Queues, topics, message handling |

## Integration with Orchestration

### During P5 (Planning)

The orchestrator should:
1. Identify technologies from the story's AC and task descriptions
2. Resolve library IDs for each relevant technology
3. Query for the specific patterns needed (e.g., "minimal API endpoint with validation")
4. Pass retrieved documentation snippets to planning agents as context

### During P7a2 (Context Externalization)

When building task briefs:
1. Identify which libraries the task's implementation steps reference
2. Query for the specific API signatures needed
3. Embed concise snippets (method signatures, config examples) in the brief's implementation steps
4. This eliminates redundant lookups during implementation

### Example: Planning a Referral Endpoint

```
1. Resolve: "ASP.NET Core" → /dotnet/aspnetcore
2. Query: "minimal API endpoint MapPost with validation"
3. Resolve: "FluentValidation" → /fluentvalidation/fluentvalidation
4. Query: "async validator with custom rules"
5. Resolve: "EF Core" → /dotnet/efcore
6. Query: "async SaveChangesAsync with owned entity"
```

Result: Planning agent receives current API patterns for all three libraries, produces a plan with accurate method signatures.

## Tips

- **Batch related queries**: If a task uses React Query + React Router, resolve both IDs first, then query each.
- **Version-specific lookups**: If you know the exact version (e.g., React Router v7), include it in the query for more precise results.
- **Prefer code mode**: For API signatures and examples. Use info mode only for architectural/conceptual questions.
- **Cache across tasks**: If multiple tasks in the same story use the same library, the documentation fetched in P5 planning should be reused in task briefs rather than re-queried.
