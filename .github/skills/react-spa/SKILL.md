---
name: react-spa
description: 'React 19 + TypeScript feature slice architecture. Functional components only, feature-based folder structure (api/components/hooks/pages/), all API calls via generated client only, explicit TypeScript interfaces on all props, React Query for server state.'
---

# React SPA Skill

## Feature Folder Structure

```
src/features/{featureName}/
├── api/                # Generated client wrappers, React Query hooks
│   └── usePatients.ts
├── components/         # Feature-specific components
│   ├── PatientCard.tsx
│   └── PatientList.tsx
├── hooks/             # Custom hooks for this feature
│   └── usePatientFilter.ts
└── pages/             # Route-level page components
    └── PatientsPage.tsx
```

## Component Rules

- **Functional components ONLY** — no class components.
- **Explicit interfaces on ALL props** — no inline types, no `any`.
- **No default exports** — use named exports exclusively.

```tsx
interface PatientCardProps {
  patient: PatientDto;
  onSelect: (id: string) => void;
  isActive?: boolean;
}

export function PatientCard({ patient, onSelect, isActive = false }: PatientCardProps) {
  return (
    <div className={cn('patient-card', { active: isActive })} onClick={() => onSelect(patient.id)}>
      <h3>{patient.fullName}</h3>
    </div>
  );
}
```

## API Calls — Generated Client Only

```tsx
// NEVER: raw fetch or axios
// ALWAYS: generated client + React Query

export function usePatients(filters: PatientFilters) {
  return useQuery({
    queryKey: ['patients', filters],
    queryFn: () => patientsClient.getPatients(filters),
  });
}
```

## State Management

- **Server state**: React Query (useQuery, useMutation)
- **Form state**: React Hook Form
- **UI state**: useState/useReducer (local only)
- **No global state libraries** unless explicitly justified
