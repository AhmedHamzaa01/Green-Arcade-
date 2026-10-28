import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminUserDetail, PointsChange } from '../../core/api/api.models';
import { UsersAdminApi } from '../../core/api/users-admin-api';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { FieldErrorPipe } from '../../shared/field-error.pipe';

/** Add or remove points with a reason (FR-07). Closes with the change. */
@Component({
  selector: 'app-adjust-points-dialog',
  imports: [
    ReactiveFormsModule, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, MatFormField,
    MatLabel, MatHint, MatError, MatInput, TranslocoPipe, FieldErrorPipe,
  ],
  template: `
    <h2 mat-dialog-title>{{ 'admin.users.adjustTitle' | transloco: { name: user.fullName } }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="stack">
        <p class="muted">{{ 'admin.users.currentBalance' | transloco: { points: user.pointsBalance } }}</p>
        <mat-form-field>
          <mat-label>{{ 'admin.users.amount' | transloco }}</mat-label>
          <input matInput type="number" formControlName="amount" cdkFocusInitial />
          <mat-hint>{{ 'admin.users.amountHint' | transloco }}</mat-hint>
          <mat-error>{{ form.controls.amount | fieldError }}</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>{{ 'admin.users.reason' | transloco }}</mat-label>
          <textarea matInput rows="2" formControlName="reason"></textarea>
          <mat-hint>{{ 'admin.users.reasonHint' | transloco }}</mat-hint>
          <mat-error>{{ form.controls.reason | fieldError }}</mat-error>
        </mat-form-field>
        @if (error()) {
          <p class="form-error" role="alert">{{ error() }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>{{ 'common.cancel' | transloco }}</button>
        <button mat-flat-button type="submit" [disabled]="saving()">{{ 'common.save' | transloco }}</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class AdjustPointsDialog {
  private readonly api = inject(UsersAdminApi);
  private readonly ref = inject(MatDialogRef<AdjustPointsDialog, PointsChange>);
  private readonly transloco = inject(TranslocoService);
  protected readonly user = inject<AdminUserDetail>(MAT_DIALOG_DATA);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    amount: [0, [Validators.required, Validators.min(-100000), Validators.max(100000)]],
    reason: ['', [Validators.required, Validators.maxLength(500)]],
  });
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async save(): Promise<void> {
    const { amount, reason } = this.form.getRawValue();
    if (!amount) {
      this.form.controls.amount.setErrors({ required: true });
    }
    if (!reason.trim()) {
      this.form.controls.reason.setErrors({ required: true });
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      this.ref.close(await firstValueFrom(this.api.adjustPoints(this.user.id, amount, reason.trim())));
    } catch (e) {
      const problem = toProblem(e);
      if (!applyServerErrors(this.form, problem)) {
        this.error.set(problem.detail ?? problem.title ?? this.transloco.translate('errors.generic'));
      }
    } finally {
      this.saving.set(false);
    }
  }
}
