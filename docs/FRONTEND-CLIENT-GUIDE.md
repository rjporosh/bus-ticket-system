# Frontend Client Guide

## Overview

The client portal (`frontend/bus-ticketing-client`) is a standalone Angular 20+ application
for public trip search and self-service booking. It runs parallel to the admin console.

## Tech Stack

- Angular 20+ with standalone components and signals
- Angular Material UI
- RxJS for async operations
- No NgRx — state is local to components/services via signals

## Project Structure

```
src/app/
  app.config.ts          # Application providers (HTTP client, interceptors, router)
  app.routes.ts          # Single source of truth for all routes
  app.component.ts       # Root component (shell)
  core/
    config/
      api-endpoints.ts   # Centralized API endpoint constants
    guards/
      auth.guard.ts      # Route guard for protected pages
    interceptors/
      http.interceptors.ts  # JWT attach + 401 refresh
    models/
      api-models.ts      # TypeScript interfaces for all API DTOs
    services/
      api.service.ts     # Generic HTTP wrapper (get/post/put/patch/delete)
      auth.service.ts    # JWT login/register/refresh/logout
      booking.service.ts # Seat map, sell tickets, cancel
      tickets.service.ts # My tickets
      trips.service.ts   # Search trips
      toast.service.ts   # Snackbar wrapper
  features/
    home/                # Landing page
    search/              # Trip search
    booking/             # Seat selection + passenger form
    my-tickets/          # Customer's own bookings
    auth/
      login/             # Customer login
      register/          # Customer registration
  layout/
    shell.component.ts   # Nav + footer wrapper
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
  },
};
```

### 2. Create or extend the service
If new domain, create `src/app/core/services/my-feature.service.ts`:
```ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../config/api-endpoints';

@Injectable({ providedIn: 'root' })
export class MyFeatureService {
  constructor(private readonly api: ApiService) {}

  list(): Observable<any[]> {
    return this.api.get(API_ENDPOINTS.myFeature.list);
  }
}
```

If extending existing service, import `API_ENDPOINTS` and use constants instead of hardcoded strings.

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
Standalone component in `src/app/features/my-feature/my-feature.component.ts`:
```ts
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MyFeatureService } from '../../core/services/my-feature.service';

@Component({
  selector: 'app-my-feature',
  standalone: true,
  imports: [CommonModule],
  template: `...`,
})
export class MyFeatureComponent implements OnInit {
  items = signal<any[]>([]);
  loading = signal(false);

  constructor(private readonly service: MyFeatureService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.list().subscribe({
      next: (data) => { this.items.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); },
    });
  }
}
```

### 5. Add models (if needed)
Edit `src/app/core/models/api-models.ts` to add TypeScript interfaces matching backend DTOs.

### 6. Build and verify
```bash
cd frontend/bus-ticketing-client
npm install
npm run build
```

## Current Key Components

- `booking.component.ts` — Seat selection with real-bus-shaped grid (driver seat, left/right blocks, aisle gap), per-seat passenger `FormArray` with "Same for all seats" toggle, mobile validation (numbers only, max 11 digits), and `SellTicketsRequest` submission
- `search.component.ts` — Trip search by date, origin, destination
- `home.component.ts` — Landing page
- `my-tickets.component.ts` — View and cancel customer bookings

## Standards

- All API paths must come from `api-endpoints.ts` — never hardcode strings
- All routes must be declared in `app.routes.ts`
- Use signals for local state, `async` pipe for templates
- Material UI components only
- Keep services thin — one method per API call
- Use `ToastService` for user notifications
