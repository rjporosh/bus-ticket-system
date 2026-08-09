import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell.component').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'booking',
        loadComponent: () => import('./features/booking/booking.component').then((m) => m.BookingComponent),
      },
      {
        path: 'stations',
        loadComponent: () => import('./features/stations/stations.component').then((m) => m.StationsComponent),
      },
      {
        path: 'routes',
        loadComponent: () => import('./features/routes-mgmt/routes.component').then((m) => m.RoutesManagementComponent),
      },
      {
        path: 'buses',
        loadComponent: () => import('./features/buses/buses.component').then((m) => m.BusesComponent),
      },
      {
        path: 'schedules',
        loadComponent: () => import('./features/schedules/schedules.component').then((m) => m.SchedulesComponent),
      },
      {
        path: 'users',
        loadComponent: () => import('./features/users/users.component').then((m) => m.UsersComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
      },
      {
        path: 'roles',
        loadComponent: () => import('./features/roles/roles.component').then((m) => m.RolesComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
