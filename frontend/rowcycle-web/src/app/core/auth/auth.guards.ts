import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { Role } from '../api/api.models';
import { AuthSession } from './auth-session';

// Guards only decide which pages to show. The real security is on the API: it answers 401/403 regardless.

/** Logged-in users only; others go to the login page and come back afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  return inject(AuthSession).isLoggedIn()
    ? true
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Pages like Login and Sign up make no sense when already logged in. */
export const guestGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(AuthSession).isLoggedIn() ? router.createUrlTree(['/account/profile']) : true;
};

/** Users with at least one of the roles. Used with `canMatch` so other users never even download the admin code. */
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
