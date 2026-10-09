import { Component, input, isDevMode } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { PageHeader } from '../../shared/page-header';

/** Shown after sign-up. */
@Component({
  selector: 'app-check-email',
  imports: [RouterLink, MatCard, MatCardContent, MatButton, MatIcon, TranslocoPipe, PageHeader],
  template: `
    <div class="page-narrow auth-page">
      <app-page-header [title]="'auth.checkEmail.title' | transloco" />
      <mat-card class="auth-card">
        <mat-card-content class="stack">
          <mat-icon class="positive">mark_email_unread</mat-icon>
          <p>{{ 'auth.checkEmail.body' | transloco: { email: email() ?? '' } }}</p>
          @if (dev) {
            <p class="muted">{{ 'auth.checkEmail.devHint' | transloco }}</p>
          }
          <a mat-flat-button routerLink="/login">{{ 'auth.checkEmail.login' | transloco }}</a>
        </mat-card-content>
      </mat-card>
    </div>
  `,
})
export class CheckEmail {
  readonly email = input<string>();
  protected readonly dev = isDevMode();
}
