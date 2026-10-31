import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSession } from '../auth/auth-session';

/** Pages like Login and Sign up make no sense when already logged in: those users go to their account. */
export const guestGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(AuthSession).isLoggedIn() ? router.createUrlTree(['/account/profile']) : true;
};
