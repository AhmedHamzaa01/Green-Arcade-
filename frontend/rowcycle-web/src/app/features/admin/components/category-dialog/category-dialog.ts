import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CatalogAdminApi } from '../../../../core/api/catalog-admin-api';
import { ProductCategory } from '../../../../core/api/models';
import { applyServerErrors, toProblem } from '../../../../core/errors/problem';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { FormDialog } from '../../../../shared/ui/form-dialog/form-dialog';

/** Create (no data) or edit (a category) — closes with the saved category. */
@Component({
  selector: 'app-category-dialog',
  imports: [
    ReactiveFormsModule, MatFormField,
    MatLabel, MatHint, MatError, MatInput, MatSlideToggle, TranslocoPipe, FieldErrorPipe, FormDialog,
  ],
  templateUrl: './category-dialog.html',
})
export class CategoryDialog {
  private readonly api = inject(CatalogAdminApi);
  private readonly ref = inject(MatDialogRef<CategoryDialog, ProductCategory>);
  private readonly transloco = inject(TranslocoService);
  protected readonly category = inject<ProductCategory | null>(MAT_DIALOG_DATA);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: [this.category?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    slug: [this.category?.slug ?? ''],
    sortOrder: [this.category?.sortOrder ?? 0, [Validators.required, Validators.min(0)]],
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
    const request = { ...value, slug: value.slug.trim() || null };
    try {
      const saved = await firstValueFrom(this.category
        ? this.api.updateCategory(this.category.id, request)
        : this.api.createCategory(request));
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
