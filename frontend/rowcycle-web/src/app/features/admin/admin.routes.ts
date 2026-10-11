import { Routes } from '@angular/router';
import { roleGuard } from '../../core/auth/auth.guards';

export const adminRoutes: Routes = [
  // Catalog: store managers and admins
  { path: 'products', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./products').then((m) => m.AdminProducts) },
  { path: 'products/new', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./product-edit').then((m) => m.ProductEdit) },
  { path: 'products/:id', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./product-edit').then((m) => m.ProductEdit) },
  { path: 'categories', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./categories').then((m) => m.AdminCategories) },
  { path: 'orders', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./orders').then((m) => m.AdminOrders) },
  { path: 'orders/:id', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./order-detail').then((m) => m.AdminOrderDetail) },

  // Admins only
  { path: 'settings', canMatch: [roleGuard('Admin')], loadComponent: () => import('./settings').then((m) => m.Settings) },
  { path: 'audit-log', canMatch: [roleGuard('Admin')], loadComponent: () => import('./audit-log').then((m) => m.AuditLog) },

  { path: '', pathMatch: 'full', redirectTo: 'products' },
];
