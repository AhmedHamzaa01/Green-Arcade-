import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  { path: 'settings', loadComponent: () => import('./settings').then((m) => m.Settings) },
  { path: 'audit-log', loadComponent: () => import('./audit-log').then((m) => m.AuditLog) },
  { path: '', pathMatch: 'full', redirectTo: 'settings' },
];
