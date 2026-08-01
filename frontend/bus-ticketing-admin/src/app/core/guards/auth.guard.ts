import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  router.navigate(['/login']);
  return false;
};

/** Usage: { path: '...', canActivate: [authGuard, roleGuard], data: { roles: ['Admin'] } } */
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const requiredRoles = (route.data['roles'] as string[] | undefined) ?? [];
  const userRole = auth.currentUser()?.role;

  if (requiredRoles.length === 0 || (userRole && requiredRoles.includes(userRole))) return true;

  router.navigate(['/dashboard']);
  return false;
};
