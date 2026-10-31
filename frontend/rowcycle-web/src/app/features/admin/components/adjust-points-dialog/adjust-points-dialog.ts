import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminUserDetail, PointsChange } from '../../../../core/api/models';
import { UsersAdminApi } from '../../../../core/api/users-admin-api';
import { applyServerErrors, toProblem } from '../../../../core/errors/problem';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { FormDialog } from '../../../../shared/ui/form-dialog/form-dialog';

/** Add or remove points with a reason (FR-07). Closes with the change. */
@Component({
  selector: 'app-adjust-points-dialog',
  imports: [
    ReactiveFormsModule, MatFormField,
    MatLabel, MatHint, MatError, MatInput, TranslocoPipe, FieldErrorPipe, FormDialog,
  ],
  templateUrl: './adjust-points-dialog.html',
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
