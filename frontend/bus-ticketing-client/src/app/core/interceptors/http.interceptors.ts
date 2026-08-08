import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, finalize, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { LoadingService } from '../services/loading.service';
import { ProblemDetails } from '../models/api-models';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject<AuthService>(AuthService);
  const token = auth.accessToken();

  const anonymousAuthPaths = ['/auth/login', '/auth/register', '/auth/refresh'];
  const isAnonymousAuth = anonymousAuthPaths.some(p => req.url.includes(p));
  const authedReq = token && !isAnonymousAuth ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authedReq).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAnonymousAuth && auth.getRefreshTokenValue()) {
        return auth.refresh().pipe(
          switchMap(() => {
            const retried = req.clone({ setHeaders: { Authorization: `Bearer ${auth.accessToken()}` } });
            return next(retried);
          }),
          catchError((refreshError) => {
            auth.logout();
            return throwError(() => refreshError);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject<LoadingService>(LoadingService);
  loading.start();
  return next(req).pipe(finalize(() => loading.stop()));
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status !== 401) {
        const problem = error.error as ProblemDetails | undefined;
        const message = problem?.detail ?? problem?.title ?? 'Something went wrong. Please try again.';
        console.error(message);
      }
      return throwError(() => error);
    }),
  );
};
