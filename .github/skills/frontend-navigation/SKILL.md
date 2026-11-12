---
name: frontend-navigation
description: 'React Router v7 patterns. Adding routes to router.tsx, lazy loading, protected routes using Azure Entra auth guard, nested layouts, navigation link patterns.'
---

# Frontend Navigation Skill

## Adding Routes

All routes are defined in `router.tsx`:

```tsx
import { createBrowserRouter } from 'react-router-dom';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      { index: true, element: <DashboardPage /> },
      {
        path: 'patients',
        lazy: () => import('./features/patients/pages/PatientsPage'),
      },
      {
        path: 'patients/:id',
        lazy: () => import('./features/patients/pages/PatientDetailPage'),
      },
    ],
  },
]);
```

## Lazy Loading

Every feature page route uses `lazy()`:

```tsx
{
  path: 'referrals',
  lazy: () => import('./features/referrals/pages/ReferralsPage'),
}
```

The lazy-loaded module must export a `Component`:

```tsx
// features/referrals/pages/ReferralsPage.tsx
export function Component() {
  return <ReferralsPageContent />;
}
Component.displayName = 'ReferralsPage';
```

## Protected Routes (Azure Entra)

```tsx
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  return (
    <>
      <AuthenticatedTemplate>{children}</AuthenticatedTemplate>
      <UnauthenticatedTemplate><RedirectToLogin /></UnauthenticatedTemplate>
    </>
  );
}
```

## Nested Layouts

```tsx
{
  path: 'admin',
  element: <ProtectedRoute><AdminLayout /></ProtectedRoute>,
  children: [
    { path: 'users', lazy: () => import('./features/admin/pages/UsersPage') },
    { path: 'settings', lazy: () => import('./features/admin/pages/SettingsPage') },
  ],
}
```

## Navigation Links

```tsx
import { NavLink } from 'react-router-dom';

<NavLink to="/patients" className={({ isActive }) => isActive ? 'active' : ''}>
  Patients
</NavLink>
```
