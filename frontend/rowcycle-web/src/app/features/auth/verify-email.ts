import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthApi } from '../../core/api/auth-api';
import { AuthSession } from '../../core/auth/auth-session';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

/** Opened from the email link: /verify-email?userId=…&token=… */
@Component({
  selector: 'app-verify-email',
  imports: [RouterLink, MatCard, MatCardContent, MatButton, MatIcon, TranslocoPipe, PageHeader, Loading],
  template: `
    <div class="page-narrow auth-page">
      <app-page-header [title]="'auth.verify.title' | transloco" />
      <mat-card class="auth-card">
        <mat-card-content class="stack">
          @switch (state()) {
            @case ('working') {
              <app-loading />
              <p class="muted">{{ 'auth.verify.working' | transloco }}</p>
            }
            @case ('success') {
              <mat-icon class="positive">verified</mat-icon>
              <p>{{ 'auth.verify.success' | transloco }}</p>
              <a mat-flat-button [routerLink]="session.isLoggedIn() ? '/account/profile' : '/login'">{{ 'auth.verify.continue' | transloco }}</a>
            }
            @case ('failed') {
              <mat-icon class="negative">error</mat-icon>
              <p>{{ 'auth.verify.failed' | transloco }}</p>
              <a mat-button routerLink="/login">{{ 'auth.checkEmail.login' | transloco }}</a>
            }
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
})
export class VerifyEmail implements OnInit {
  private readonly api = inject(AuthApi);
  protected readonly session = inject(AuthSession);

  readonly userId = input<string>();
  readonly token = input<string>();
  protected readonly state = signal<'working' | 'success' | 'failed'>('working');

  async ngOnInit(): Promise<void> {
    const userId = this.userId();
    const token = this.token();
    if (!userId || !token) {
      this.state.set('failed');
      return;
    }

    try {
      await firstValueFrom(this.api.verifyEmail({ userId, token }));
      this.state.set('success');
      if (this.session.isLoggedIn()) {
        // Get a token that says "email verified" and refresh the profile.
        await firstValueFrom(this.session.refreshAccessToken());
        await this.session.reloadUser();
      }
    } catch {
      this.state.set('failed');
    }
  }
}
