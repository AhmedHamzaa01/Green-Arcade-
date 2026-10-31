import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards';

/** The member's own account (F1, F2, F7): logged-in members only. */
export const accountRoutes: Routes = [
  {
    path: 'account',
    canActivate: [authGuard],
    children: [
      { path: 'profile', loadComponent: () => import('./pages/profile/profile').then((m) => m.Profile) },
      { path: 'points', loadComponent: () => import('./pages/points-history/points-history').then((m) => m.PointsHistory) },
      { path: 'orders', loadComponent: () => import('./pages/my-orders/my-orders').then((m) => m.MyOrders) },
      { path: 'orders/:id', loadComponent: () => import('./pages/my-order/my-order').then((m) => m.MyOrder) },
      { path: 'addresses', loadComponent: () => import('./pages/addresses/addresses').then((m) => m.Addresses) },
      { path: '', pathMatch: 'full', redirectTo: 'profile' },
    ],
  },
];
