import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminApi } from '../../core/api/admin-api';
import { Notifier } from '../../core/errors/notifier';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { FieldErrorPipe } from '../../shared/field-error.pipe';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

/** GET/PUT /admin/settings. Each change is audit-logged by the API. */
@Component({
  selector: 'app-admin-settings',
  imports: [
    ReactiveFormsModule, RouterLink, MatCard, MatCardContent, MatFormField, MatLabel, MatHint, MatInput, MatError,
    MatButton, TranslocoPipe, FieldErrorPipe, PageHeader, Loading,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.settings.title' | transloco">
        <a mat-button routerLink="/admin/audit-log">{{ 'nav.auditLog' | transloco }}</a>
      </app-page-header>
      @if (loaded()) {
        <mat-card>
          <mat-card-content>
            <form class="stack settings-form" [formGroup]="form" (ngSubmit)="save()">
              <mat-form-field>
                <mat-label>{{ 'admin.settings.dailySubmissionLimit' | transloco }}</mat-label>
                <input matInput type="number" min="1" max="50" formControlName="dailySubmissionLimit" />
                <mat-hint>{{ 'admin.settings.dailySubmissionLimitHint' | transloco }}</mat-hint>
                <mat-error>{{ form.controls.dailySubmissionLimit | fieldError }}</mat-error>
              </mat-form-field>
              @if (error()) {
                <p class="form-error" role="alert">{{ error() }}</p>
              }
              <div class="row">
                <button mat-flat-button type="submit" [disabled]="saving() || form.pristine">{{ 'common.save' | transloco }}</button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>
      } @else {
        <app-loading />
      }
    </div>
  `,
  styles: `.settings-form { max-width: 360px; }`,
})
export class Settings implements OnInit {
  private readonly api = inject(AdminApi);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    dailySubmissionLimit: [5, [Validators.required, Validators.min(1), Validators.max(50)]],
  });
  protected readonly loaded = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    this.form.reset(await firstValueFrom(this.api.getSettings()));
    this.loaded.set(true);
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      const saved = await firstValueFrom(this.api.updateSettings(this.form.getRawValue()));
      this.form.reset(saved);
      this.notifier.success('admin.settings.saved');
    } catch (e) {
      const problem = toProblem(e);
      if (!applyServerErrors(this.form, problem)) {
        this.error.set(problem.title ?? this.transloco.translate('errors.generic'));
      }
    } finally {
      this.saving.set(false);
    }
  }
}
