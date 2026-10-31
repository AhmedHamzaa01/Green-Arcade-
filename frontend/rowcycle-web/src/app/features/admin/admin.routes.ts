import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards';
import { StaffLayout } from '../../layout/staff-layout/staff-layout';

/**
 * The staff area (F8). Every page renders inside the staff layout (side panel), and each page's `canMatch`
 * lists the roles allowed to open it. app.routes.ts already lets only staff into /admin at all.
 */
export const adminRoutes: Routes = [{ path: '', component: StaffLayout, children: [
  // Catalog: store managers and admins
  { path: 'products', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./pages/products/products').then((m) => m.AdminProducts) },
  { path: 'products/new', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./pages/product-edit/product-edit').then((m) => m.ProductEdit) },
  { path: 'products/:id', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./pages/product-edit/product-edit').then((m) => m.ProductEdit) },
  { path: 'categories', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./pages/categories/categories').then((m) => m.AdminCategories) },
  { path: 'orders', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./pages/orders/orders').then((m) => m.AdminOrders) },
  { path: 'orders/:id', canMatch: [roleGuard('StoreManager', 'Admin')], loadComponent: () => import('./pages/order-detail/order-detail').then((m) => m.AdminOrderDetail) },

  // Moderation: moderators and admins
  { path: 'submissions', canMatch: [roleGuard('Moderator', 'Admin')], loadComponent: () => import('./pages/submissions/submissions').then((m) => m.AdminSubmissions) },
  { path: 'submissions/:id', canMatch: [roleGuard('Moderator', 'Admin')], loadComponent: () => import('./pages/submission-review/submission-review').then((m) => m.SubmissionReview) },

  // Admins only
  { path: 'settings', canMatch: [roleGuard('Admin')], loadComponent: () => import('./pages/settings/settings').then((m) => m.Settings) },
  { path: 'audit-log', canMatch: [roleGuard('Admin')], loadComponent: () => import('./pages/audit-log/audit-log').then((m) => m.AuditLog) },
  { path: 'badges', canMatch: [roleGuard('Admin')], loadComponent: () => import('./pages/badges/badges').then((m) => m.AdminBadges) },
  { path: 'submission-categories', canMatch: [roleGuard('Admin')], loadComponent: () => import('./pages/submission-categories/submission-categories').then((m) => m.AdminSubmissionCategories) },
  { path: 'users', canMatch: [roleGuard('Admin')], loadComponent: () => import('./pages/users/users').then((m) => m.AdminUsers) },
  { path: 'users/:id', canMatch: [roleGuard('Admin')], loadComponent: () => import('./pages/user-detail/user-detail').then((m) => m.AdminUserDetailPage) },

  // Every staff role starts on the dashboard.
  { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.AdminDashboard) },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
] }];
