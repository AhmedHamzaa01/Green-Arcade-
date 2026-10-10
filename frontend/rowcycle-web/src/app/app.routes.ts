import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth/auth.guards';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/home/home').then((m) => m.Home) },

  // Store (F5)
  { path: 'products', loadComponent: () => import('./features/store/product-list').then((m) => m.ProductList) },
  { path: 'products/:slug', loadComponent: () => import('./features/store/product-detail').then((m) => m.ProductDetailPage) },

  // Auth (F1)
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./features/auth/login').then((m) => m.Login) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./features/auth/register').then((m) => m.Register) },
  { path: 'check-email', loadComponent: () => import('./features/auth/check-email').then((m) => m.CheckEmail) },
  // Opened from the emailed links; the API builds them as /verify-email?userId=…&token=… and /reset-password?email=…&token=…
  { path: 'verify-email', loadComponent: () => import('./features/auth/verify-email').then((m) => m.VerifyEmail) },
  { path: 'forgot-password', canActivate: [guestGuard], loadComponent: () => import('./features/auth/forgot-password').then((m) => m.ForgotPassword) },
  { path: 'reset-password', loadComponent: () => import('./features/auth/reset-password').then((m) => m.ResetPassword) },

  // Account (F1, F2)
  {
    path: 'account',
    canActivate: [authGuard],
    children: [
      { path: 'profile', loadComponent: () => import('./features/account/profile').then((m) => m.Profile) },
      { path: 'points', loadComponent: () => import('./features/account/points-history').then((m) => m.PointsHistory) },
      { path: '', pathMatch: 'full', redirectTo: 'profile' },
    ],
  },

  // Staff area (F8): only staff even load this code; each page also checks its own roles.
  { path: 'admin', canMatch: [roleGuard('Admin', 'StoreManager')], loadChildren: () => import('./features/admin/admin.routes').then((m) => m.adminRoutes) },

  { path: '**', loadComponent: () => import('./features/not-found').then((m) => m.NotFound) },
];
