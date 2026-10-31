import { Routes } from '@angular/router';
import { roleGuard } from './core/guards';
import { accountRoutes } from './features/account/account.routes';
import { actionsRoutes } from './features/actions/actions.routes';
import { authRoutes } from './features/auth/auth.routes';
import { homeRoutes } from './features/home/home.routes';
import { notFoundRoutes } from './features/not-found/not-found.routes';
import { storeRoutes } from './features/store/store.routes';

/**
 * All routes. Each feature lists its own pages and the guards that protect them in `features/<area>/<area>.routes.ts`;
 * this file only puts them together. Guards decide which pages to show; the API checks every request again.
 */
export const routes: Routes = [
  ...homeRoutes,
  ...storeRoutes,
  ...authRoutes,
  ...accountRoutes,
  ...actionsRoutes,

  // Staff area (F8): only staff even download this code; each page also checks its own roles (admin.routes.ts).
  {
    path: 'admin',
    canMatch: [roleGuard('Admin', 'StoreManager', 'Moderator')],
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.adminRoutes),
  },

  // Last: anything no route above matches.
  ...notFoundRoutes,
];
