import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'home' },
      {
        path: 'home',
        loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
      },
      {
        path: 'search',
        loadComponent: () => import('./features/search/search.component').then((m) => m.SearchComponent),
      },
      {
        path: 'booking/:tripId',
        loadComponent: () => import('./features/booking/booking.component').then((m) => m.BookingComponent),
      },
      {
        path: 'my-tickets',
        loadComponent: () => import('./features/my-tickets/my-tickets.component').then((m) => m.MyTicketsComponent),
      },
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'home' },
];