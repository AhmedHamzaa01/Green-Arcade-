import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { SKIP_ERROR_TOAST } from '../http-context';
import { Notifier } from './notifier';
import { toProblem } from './problem';

/**
 * Shows a snackbar for API errors that the page doesn't handle itself.
 * 401 is left to the auth interceptor (refresh or go to login).
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const notifier = inject(Notifier);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status !== 401 && !request.context.get(SKIP_ERROR_TOAST)) {
        notifier.error(messageFor(error));
      }
      return throwError(() => error);
    }),
  );
};

function messageFor(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'errors.network';
  }
  if (error.status === 403) {
    return 'errors.forbidden';
  }
  const problem = toProblem(error);
  if (!problem.title) {
    return 'errors.generic';
  }
  return problem.detail ? `${problem.title} ${problem.detail}` : problem.title;
}
