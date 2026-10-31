import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { Role } from '../api/models';
import { AuthSession } from '../auth/auth-session';

/**
 * Users with at least one of the roles. Used with `canMatch`, so other users never even download that code.
 * Logged-out users go to the login page; logged-in users without the role go home.
 */
export function roleGuard(...roles: Role[]): CanMatchFn {
  return (_route, segments) => {
    const session = inject(AuthSession);
    const router = inject(Router);
    if (!session.isLoggedIn()) {
      const returnUrl = '/' + segments.map((s) => s.path).join('/');
      return router.createUrlTree(['/login'], { queryParams: { returnUrl } });
    }
    return session.hasRole(...roles) ? true : router.createUrlTree(['/']);
  };
}
