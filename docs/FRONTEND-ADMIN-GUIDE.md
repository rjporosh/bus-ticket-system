# Frontend Admin Guide

## Overview

The admin console (`frontend/bus-ticketing-admin`) is a standalone Angular 20+ application
for fleet management, ticket selling, and dashboard analytics.

## Tech Stack

- Angular 20+ with standalone components and signals
- Angular Material UI
- RxJS for async operations
- No NgRx — state is local to components/services via signals

## Project Structure

```
src/app/
  app.config.ts           # Application providers
  app.routes.ts           # Single source of truth for all routes
  app.component.ts        # Root component
  core/
    config/
      api-endpoints.ts    # Centralized API endpoint constants
    guards/
      auth.guard.ts       # authGuard + roleGuard
    interceptors/
      http.interceptors.ts  # JWT attach + 401 refresh
    models/
      api-models.ts       # TypeScript interfaces for all API DTOs
    services/
      api.service.ts      # Generic HTTP wrapper
      auth.service.ts     # JWT login/refresh/logout
      feature-services.ts # All domain services (Users, Roles, Stations, Routes, Buses, Schedules, Booking, Dashboard)
      toast.service.ts    # Snackbar wrapper
      loading.service.ts  # Global loading indicator
  features/
    auth/
      login/              # Admin/staff login
    dashboard/            # Sales and seat status overview
    booking/              # Ticket selling + seat map
    stations/             # Station CRUD
    routes-mgmt/          # Route CRUD
    buses/                # Bus CRUD + seat layout management
    schedules/            # Schedule CRUD
    users/                # User CRUD (Admin only)
    roles/                # Role CRUD (Admin only)
  layout/
    shell.component.ts    # Nav + sidebar wrapper
```

## How to Add a New Feature (Step by Step)

### 1. Add API endpoint constant
Edit `src/app/core/config/api-endpoints.ts`:
```ts
export const API_ENDPOINTS = {
  // ... existing
  myFeature: {
    list: '/my-feature',
    get: (id: string) => `/my-feature/${id}`,
    create: '/my-feature',
    update: (id: string) => `/my-feature/${id}`,
    delete: (id: string) => `/my-feature/${id}`,
  },
};
```

### 2. Add service method
Edit `src/app/core/services/feature-services.ts` (or create a new service file):
```ts
@Injectable({ providedIn: 'root' })
export class MyFeatureService {
  constructor(private readonly api: ApiService) {}

  list(): Observable<any[]> {
    return this.api.get(API_ENDPOINTS.myFeature.list);
  }
}
```

### 3. Add route
Edit `src/app/app.routes.ts`:
```ts
{
  path: 'my-feature',
  loadComponent: () => import('./features/my-feature/my-feature.component').then(m => m.MyFeatureComponent),
  canActivate: [authGuard],
}
```

### 4. Create the component
Standalone component in `src/app/features/my-feature/my-feature.component.ts`.

### 5. Add models (if needed)
Edit `src/app/core/models/api-models.ts` to match backend DTOs.

### 6. Build and verify
```bash
cd frontend/bus-ticketing-admin
npm install
npm run build
```

## Standards

- All API paths must come from `api-endpoints.ts` — never hardcode strings
- All routes must be declared in `app.routes.ts`
- Use `roleGuard` with `data: { roles: ['Admin'] }` for admin-only pages
- Use Material table (`<mat-table>`) for list pages
- Use `ToastService` for success/error notifications
- Use `LoadingService` for global loading state
