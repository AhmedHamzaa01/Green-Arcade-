import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AuthSession } from '../../core/auth/auth-session';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { FieldErrorPipe } from '../../shared/field-error.pipe';
import { PageHeader } from '../../shared/page-header';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule, RouterLink, MatCard, MatCardContent, MatFormField, MatLabel, MatInput, MatError, MatButton,
    TranslocoPipe, FieldErrorPipe, PageHeader,
  ],
  template: `
    <div class="page-narrow auth-page">
      <app-page-header [title]="'auth.login.title' | transloco" />
      <mat-card class="auth-card">
        <mat-card-content>
          <form class="stack" [formGroup]="form" (ngSubmit)="submit()">
            <mat-form-field>
              <mat-label>{{ 'auth.email' | transloco }}</mat-label>
              <input matInput type="email" formControlName="email" autocomplete="email" />
              <mat-error>{{ form.controls.email | fieldError }}</mat-error>
            </mat-form-field>
            <mat-form-field>
              <mat-label>{{ 'auth.password' | transloco }}</mat-label>
              <input matInput type="password" formControlName="password" autocomplete="current-password" />
              <mat-error>{{ form.controls.password | fieldError }}</mat-error>
            </mat-form-field>
            @if (error()) {
              <p class="form-error" role="alert">{{ error() }}</p>
            }
            <button mat-flat-button type="submit" [disabled]="submitting()">{{ 'auth.login.submit' | transloco }}</button>
          </form>
          <div class="card-links">
            <a routerLink="/forgot-password">{{ 'auth.login.forgot' | transloco }}</a>
            <span class="muted">{{ 'auth.login.noAccount' | transloco }} <a routerLink="/register">{{ 'auth.login.register' | transloco }}</a></span>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
})
export class Login {
  private readonly session = inject(AuthSession);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);

  /** From `?returnUrl=`: where to go after logging in (set by the guards). */
  readonly returnUrl = input<string>();

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.session.login(this.form.getRawValue());
      await this.router.navigateByUrl(this.returnUrl() || '/account/profile');
    } catch (e) {
      const problem = toProblem(e);
      if (!applyServerErrors(this.form, problem)) {
        // e.g. "Invalid email or password." or "Account locked." from the API
        this.error.set(problem.title ?? this.transloco.translate('errors.generic'));
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
