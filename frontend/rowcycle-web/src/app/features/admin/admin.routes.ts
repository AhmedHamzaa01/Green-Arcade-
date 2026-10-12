import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { roleGuard } from '../../core/auth/auth.guards';
import { AuthSession } from '../../core/auth/auth-session';

export const adminRoutes: Routes = [
  // Catalog: store managers and admins
  { path: 'products', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./products').then((m) => m.AdminProducts) },
  { path: 'products/new', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./product-edit').then((m) => m.ProductEdit) },
  { path: 'products/:id', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./product-edit').then((m) => m.ProductEdit) },
  { path: 'categories', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./categories').then((m) => m.AdminCategories) },
  { path: 'orders', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./orders').then((m) => m.AdminOrders) },
  { path: 'orders/:id', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./order-detail').then((m) => m.AdminOrderDetail) },

  // Moderation: moderators and admins
  { path: 'submissions', canMatch: [roleGuard('Moderator', 'Admin')], loadComponent: () => import('./submissions').then((m) => m.AdminSubmissions) },
  { path: 'submissions/:id', canMatch: [roleGuard('Moderator', 'Admin')], loadComponent: () => import('./submission-review').then((m) => m.SubmissionReview) },

  // Admins only
  { path: 'settings', canMatch: [roleGuard('Admin')], loadComponent: () => import('./settings').then((m) => m.Settings) },
  { path: 'audit-log', canMatch: [roleGuard('Admin')], loadComponent: () => import('./audit-log').then((m) => m.AuditLog) },

  // Each role lands on its own first page.
  { path: '', pathMatch: 'full', redirectTo: () => (inject(AuthSession).hasRole('Admin', 'StoreManager') ? 'products' : 'submissions') },
];
