import { Routes } from '@angular/router';
import { guestGuard } from '../../core/guards';

/**
 * Sign-up and login (F1). Login, Sign up and Forgot password are for visitors only (guestGuard sends logged-in users
 * to their account). The emailed links (/verify-email?userId=…&token=…, /reset-password?email=…&token=…) open for anyone.
 */
export const authRoutes: Routes = [
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./pages/login/login').then((m) => m.Login) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./pages/register/register').then((m) => m.Register) },
  { path: 'forgot-password', canActivate: [guestGuard], loadComponent: () => import('./pages/forgot-password/forgot-password').then((m) => m.ForgotPassword) },
  { path: 'check-email', loadComponent: () => import('./pages/check-email/check-email').then((m) => m.CheckEmail) },
  { path: 'verify-email', loadComponent: () => import('./pages/verify-email/verify-email').then((m) => m.VerifyEmail) },
  { path: 'reset-password', loadComponent: () => import('./pages/reset-password/reset-password').then((m) => m.ResetPassword) },
];
