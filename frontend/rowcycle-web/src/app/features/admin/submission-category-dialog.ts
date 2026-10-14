import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminSubmissionCategory } from '../../core/api/api.models';
import { SubmissionCategoriesAdminApi } from '../../core/api/submissions-api';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { FieldErrorPipe } from '../../shared/field-error.pipe';

/** Create (no data) or edit (a category) — closes with the saved category. */
@Component({
  selector: 'app-submission-category-dialog',
  imports: [
    ReactiveFormsModule, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, MatFormField,
    MatLabel, MatHint, MatError, MatInput, MatSlideToggle, TranslocoPipe, FieldErrorPipe,
  ],
  template: `
    <h2 mat-dialog-title>{{ (category ? 'admin.actionCategories.edit' : 'admin.actionCategories.new') | transloco }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="stack">
        <mat-form-field>
          <mat-label>{{ 'admin.actionCategories.name' | transloco }}</mat-label>
          <input matInput formControlName="name" />
          <mat-error>{{ form.controls.name | fieldError }}</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>{{ 'admin.actionCategories.points' | transloco }}</mat-label>
          <input matInput type="number" min="0" max="10000" formControlName="pointsReward" />
          <mat-hint>{{ 'admin.actionCategories.pointsHint' | transloco }}</mat-hint>
          <mat-error>{{ form.controls.pointsReward | fieldError }}</mat-error>
        </mat-form-field>
        <mat-slide-toggle formControlName="tracksWeight">{{ 'admin.actionCategories.tracksWeight' | transloco }}</mat-slide-toggle>
        <mat-slide-toggle formControlName="isActive">{{ 'admin.actionCategories.active' | transloco }}</mat-slide-toggle>
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
