import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, finalize, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { LoadingService } from '../services/loading.service';
import { ToastService } from '../services/toast.service';
import { ProblemDetails } from '../models/api-models';

/** Attaches the bearer access token; on a 401 (expired token) attempts one silent refresh-and-retry before giving up. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject<AuthService>(AuthService);
  const token = auth.accessToken();

  const anonymousAuthPaths = ['/auth/login', '/auth/register', '/auth/refresh'];
  const isAnonymousAuth = anonymousAuthPaths.some(p => req.url.includes(p));
  const authedReq = token && !isAnonymousAuth ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authedReq).pipe(
    catchError((error: unknown) => {
      const isAuthEndpoint = req.url.includes('/auth/');
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

/** Tracks in-flight requests for the global top-loading-bar. */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject<LoadingService>(LoadingService);
  loading.start();
  return next(req).pipe(finalize(() => loading.stop()));
};

/** Surfaces API errors as toasts, using ASP.NET Core's ProblemDetails shape when available. */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject<ToastService>(ToastService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status !== 401) {
        const problem = error.error as ProblemDetails | undefined;
        const message = problem?.detail ?? problem?.title ?? 'Something went wrong. Please try again.';
        toast.error(message);
      }
      return throwError(() => error);
    }),
  );
};
