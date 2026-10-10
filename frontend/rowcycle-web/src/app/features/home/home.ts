import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthSession } from '../../core/auth/auth-session';

/** Placeholder home page until the store exists (Step 12). */
@Component({
  selector: 'app-home',
  imports: [RouterLink, MatCard, MatCardContent, MatButton, MatIcon, TranslocoPipe],
  template: `
    <div class="page">
      <mat-card>
        <mat-card-content class="stack hero">
          <mat-icon class="positive">eco</mat-icon>
          <h1>{{ 'home.title' | transloco }}</h1>
          <p>{{ 'home.intro' | transloco }}</p>
        </mat-card-content>
      </mat-card>

      <!-- Actions sit below the welcome card -->
      <div class="actions">
        <a mat-flat-button routerLink="/products">{{ 'home.browse' | transloco }}</a>
        @if (session.isLoggedIn()) {
          <a mat-stroked-button routerLink="/account/profile">{{ 'home.goToProfile' | transloco }}</a>
        } @else {
          <a mat-stroked-button routerLink="/register">{{ 'home.start' | transloco }}</a>
          <a mat-stroked-button routerLink="/login">{{ 'nav.login' | transloco }}</a>
        }
      </div>
    </div>
  `,
  styles: `
    .hero { padding: 16px 8px; }
    h1 { font: var(--mat-sys-headline-medium); margin: 0; }
    mat-icon { font-size: 48px; width: 48px; height: 48px; }
    .actions { display: flex; justify-content: center; gap: 12px; flex-wrap: wrap; margin-top: 24px; }
  `,
})
export class Home {
  protected readonly session = inject(AuthSession);
}
