---
description: "Use when naming files, classes, methods, variables, projects, folders, or events. Covers C# and TypeScript naming conventions for the MOW 2.0 project."
---
# Naming Conventions

## C# (.NET Backend)

| Element | Convention | Example |
|---------|-----------|---------|
| Classes / Records | PascalCase | `CreateReferralHandler` |
| Interfaces | I-prefix + PascalCase | `IReferralRepository` |
| Methods | PascalCase | `GetByIdAsync` |
| Async methods | PascalCase + `Async` suffix | `CreateReferralAsync` |
| Properties | PascalCase | `PatientName` |
| Private fields | `_camelCase` | `_referralRepository` |
| Constants | PascalCase | `MaxRetryCount` |
| Enums | PascalCase (singular) | `ReferralStatus` |
| Enum values | PascalCase | `ReferralStatus.PendingReview` |

### DTOs and Requests

- Suffix request DTOs with `Request`: `CreateReferralRequest`
- Suffix response DTOs with `Response`: `ReferralDetailResponse`
- Suffix commands with `Command`: `CreateReferralCommand`
- Suffix queries with `Query`: `GetReferralByIdQuery`

### Domain Events

- Past tense describing what happened: `ReferralCreatedEvent`, `StatusChangedEvent`
- Always suffix with `Event`

### Projects / Modules

```
{ModuleName}.Domain
{ModuleName}.Application
{ModuleName}.Infrastructure
{ModuleName}.Contracts
```

---

## TypeScript (Frontend)

| Element | Convention | Example |
|---------|-----------|---------|
| Components | PascalCase | `ReferralList` |
| Component files | PascalCase `.tsx` | `ReferralList.tsx` |
| Hooks | `use` prefix + PascalCase | `useReferralList` |
| Hook files | camelCase `.ts` | `useReferralList.ts` |
| Interfaces / Types | PascalCase | `ReferralListProps` |
| Variables / Functions | camelCase | `getReferralStatus` |
| Constants | SCREAMING_SNAKE_CASE | `MAX_PAGE_SIZE` |
| Feature folders | kebab-case | `referrals/`, `patient-intake/` |
| Utility files | camelCase `.ts` | `formatDate.ts` |

### Props Interfaces

- Suffix component prop types with `Props`: `ReferralListProps`
- Always explicitly define — no inline object types for component props

### API Hooks

- Prefix with `use` + action + resource: `useCreateReferral`, `useFetchReferralList`
- Return React Query results directly
