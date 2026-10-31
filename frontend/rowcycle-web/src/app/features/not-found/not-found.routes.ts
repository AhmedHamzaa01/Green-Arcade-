import { Routes } from '@angular/router';

/** Anything no other route matches. Must be the last entry in app.routes.ts. */
export const notFoundRoutes: Routes = [
  { path: '**', loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFound) },
];
