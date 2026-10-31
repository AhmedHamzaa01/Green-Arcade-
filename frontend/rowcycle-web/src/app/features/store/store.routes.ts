import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards';

/** The store (F5) is public; the cart and checkout (F6) need a logged-in member. */
export const storeRoutes: Routes = [
  { path: 'products', loadComponent: () => import('./pages/product-list/product-list').then((m) => m.ProductList) },
  { path: 'products/:slug', loadComponent: () => import('./pages/product-detail/product-detail').then((m) => m.ProductDetailPage) },
  { path: 'cart', canActivate: [authGuard], loadComponent: () => import('./pages/cart/cart').then((m) => m.CartPage) },
  { path: 'checkout', canActivate: [authGuard], loadComponent: () => import('./pages/checkout/checkout').then((m) => m.Checkout) },
];
