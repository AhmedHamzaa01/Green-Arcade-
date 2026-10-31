import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminSubmissionCategory } from '../../../../core/api/models';
import { SubmissionCategoriesAdminApi } from '../../../../core/api/submissions-api';
import { applyServerErrors, toProblem } from '../../../../core/errors/problem';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { FormDialog } from '../../../../shared/ui/form-dialog/form-dialog';

/** Create (no data) or edit (a category) — closes with the saved category. */
@Component({
  selector: 'app-submission-category-dialog',
  imports: [
    ReactiveFormsModule, MatFormField,
    MatLabel, MatHint, MatError, MatInput, MatSlideToggle, TranslocoPipe, FieldErrorPipe, FormDialog,
  ],
  templateUrl: './submission-category-dialog.html',
})
export class SubmissionCategoryDialog {
  private readonly api = inject(SubmissionCategoriesAdminApi);
  private readonly ref = inject(MatDialogRef<SubmissionCategoryDialog, AdminSubmissionCategory>);
  private readonly transloco = inject(TranslocoService);
  protected readonly category = inject<AdminSubmissionCategory | null>(MAT_DIALOG_DATA);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: [this.category?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    pointsReward: [this.category?.pointsReward ?? 10, [Validators.required, Validators.min(0), Validators.max(10000)]],
    tracksWeight: [this.category?.tracksWeight ?? false],
    isActive: [this.category?.isActive ?? true],
  });
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    const request = { ...value, name: value.name.trim() };
    try {
      const saved = await firstValueFrom(this.category ? this.api.update(this.category.id, request) : this.api.create(request));
      this.ref.close(saved);
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
