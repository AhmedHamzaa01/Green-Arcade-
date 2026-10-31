import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards';

/** Photo-verified actions (F3): logged-in members. Submitting also needs a confirmed email; the page explains when it's missing. */
export const actionsRoutes: Routes = [
  {
    path: 'actions',
    canActivate: [authGuard],
    children: [
      { path: 'new', loadComponent: () => import('./pages/new-submission/new-submission').then((m) => m.NewSubmission) },
      { path: 'mine', loadComponent: () => import('./pages/my-submissions/my-submissions').then((m) => m.MySubmissions) },
      { path: '', pathMatch: 'full', redirectTo: 'mine' },
    ],
  },
];
