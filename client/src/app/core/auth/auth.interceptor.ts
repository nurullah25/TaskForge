import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

// Requests that must never trigger a refresh-and-retry, or we'd loop on a dead session.
const AUTH_ENDPOINTS = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh', '/api/auth/logout'];

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);

  if (!request.url.startsWith('/api/') || AUTH_ENDPOINTS.includes(request.url)) {
    return next(request);
  }

  return next(withToken(request, auth.token)).pipe(
    catchError((error) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      // Access token expired: get a new one and retry the request once.
      return auth.refresh().pipe(
        catchError((refreshError) => {
          auth.handleSessionExpired();
          return throwError(() => refreshError);
        }),
        switchMap((session) => next(withToken(request, session.accessToken))),
      );
    }),
  );
};

function withToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}
