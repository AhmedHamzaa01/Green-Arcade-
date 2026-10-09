import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { SKIP_AUTH } from '../http-context';
import { AuthSession } from './auth-session';

/**
 * Adds `Authorization: Bearer <token>` to API calls. When the API answers 401 (the 15-minute token expired),
 * it refreshes the token once and retries. If the refresh fails too, the user is sent to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(SKIP_AUTH) || !request.url.startsWith('/api/')) {
    return next(request);
  }

  const session = inject(AuthSession);
  const router = inject(Router);

  return next(withToken(request, session.token())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return session.refreshAccessToken().pipe(
        catchError(() => {
          session.clear();
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => error);
        }),
        switchMap((token) => next(withToken(request, token))),
      );
    }),
  );
};

function withToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}
