import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CatalogAdminApi } from '../../core/api/catalog-admin-api';
import { AdminProduct, AdminProductVariant } from '../../core/api/api.models';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { FieldErrorPipe } from '../../shared/field-error.pipe';

export interface VariantDialogData {
  productId: string;
  variant: AdminProductVariant | null;
}

/** Add or edit a variant (name, SKU, stock, own price). Closes with the updated product. */
@Component({
  selector: 'app-variant-dialog',
  imports: [
    ReactiveFormsModule, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, MatFormField,
    MatLabel, MatHint, MatError, MatInput, TranslocoPipe, FieldErrorPipe,
  ],
  template: `
    <h2 mat-dialog-title>{{ (data.variant ? 'admin.catalog.edit.editVariant' : 'admin.catalog.edit.addVariant') | transloco }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="stack">
        <mat-form-field>
          <mat-label>{{ 'admin.catalog.edit.variantName' | transloco }}</mat-label>
          <input matInput formControlName="name" />
          <mat-error>{{ form.controls.name | fieldError }}</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>{{ 'admin.catalog.edit.sku' | transloco }}</mat-label>
          <input matInput formControlName="sku" />
          <mat-error>{{ form.controls.sku | fieldError }}</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>{{ 'admin.catalog.edit.stock' | transloco }}</mat-label>
          <input matInput type="number" min="0" formControlName="stock" />
          <mat-error>{{ form.controls.stock | fieldError }}</mat-error>
        </mat-form-field>
        <mat-form-field>
          <mat-label>{{ 'admin.catalog.edit.priceOverride' | transloco }}</mat-label>
          <input matInput type="number" min="0" step="0.01" formControlName="priceOverride" />
          <mat-hint>{{ 'admin.catalog.edit.priceOverrideHint' | transloco }}</mat-hint>
          <mat-error>{{ form.controls.priceOverride | fieldError }}</mat-error>
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
export class VariantDialog {
  private readonly api = inject(CatalogAdminApi);
  private readonly ref = inject(MatDialogRef<VariantDialog, AdminProduct>);
  private readonly transloco = inject(TranslocoService);
  protected readonly data = inject<VariantDialogData>(MAT_DIALOG_DATA);

  protected readonly form = inject(FormBuilder).group({
    name: [this.data.variant?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    sku: [this.data.variant?.sku ?? '', [Validators.required, Validators.maxLength(64), Validators.pattern(/^[A-Za-z0-9_-]+$/)]],
    stock: [this.data.variant?.stock ?? 0, [Validators.required, Validators.min(0)]],
    priceOverride: [this.data.variant?.priceOverride ?? (null as number | null), [Validators.min(0)]],
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
    const v = this.form.getRawValue();
    const request = { name: v.name ?? '', sku: v.sku ?? '', stock: Number(v.stock ?? 0), priceOverride: v.priceOverride ?? null };
    try {
      const product = await firstValueFrom(this.data.variant
        ? this.api.updateVariant(this.data.productId, this.data.variant.id, request)
        : this.api.addVariant(this.data.productId, request));
      this.ref.close(product);
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
