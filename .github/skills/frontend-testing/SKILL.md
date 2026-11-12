---
name: frontend-testing
description: 'Vitest + React Testing Library patterns. Component and hook tests, mocking the generated API client, waitFor for async, accessibility queries, coverage thresholds.'
---

# Frontend Testing Skill

## Test Structure

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

describe('PatientCard', () => {
  it('renders patient name and triggers onSelect', async () => {
    const onSelect = vi.fn();
    render(<PatientCard patient={mockPatient} onSelect={onSelect} />);

    expect(screen.getByText('John Doe')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button'));
    expect(onSelect).toHaveBeenCalledWith('patient-123');
  });
});
```

## Accessibility Queries (Preferred Order)

1. `getByRole` — most accessible
2. `getByLabelText` — form elements
3. `getByPlaceholderText` — inputs
4. `getByText` — non-interactive elements
5. `getByTestId` — last resort only

## Async Testing

```tsx
it('loads and displays patients', async () => {
  render(<PatientsPage />);

  // Wait for async data
  await waitFor(() => {
    expect(screen.getByText('John Doe')).toBeInTheDocument();
  });
});
```

## Mocking the Generated API Client

```tsx
vi.mock('../api/patientsClient', () => ({
  patientsClient: {
    getPatients: vi.fn().mockResolvedValue({ data: [mockPatient] }),
    createPatient: vi.fn().mockResolvedValue({ data: { id: 'new-123' } }),
  },
}));
```

## Hook Testing

```tsx
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

function wrapper({ children }: { children: React.ReactNode }) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

it('fetches patients', async () => {
  const { result } = renderHook(() => usePatients({}), { wrapper });
  await waitFor(() => expect(result.current.isSuccess).toBe(true));
  expect(result.current.data).toHaveLength(1);
});
```

## Coverage Thresholds

- Components: 80%+ branch coverage
- Hooks: 90%+ branch coverage
- Pages: Integration test for primary user flow
