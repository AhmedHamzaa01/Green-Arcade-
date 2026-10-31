import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Address } from '../../../../core/api/models';
import { ShopApi } from '../../../../core/api/shop-api';
import { applyServerErrors, toProblem } from '../../../../core/errors/problem';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { FormDialog } from '../../../../shared/ui/form-dialog/form-dialog';

/** Add (no data) or edit an address. Closes with the saved address. Used on the Addresses page and in checkout. */
@Component({
  selector: 'app-address-dialog',
  imports: [ReactiveFormsModule, MatCheckbox, MatFormField, MatLabel, MatError, MatInput, TranslocoPipe, FieldErrorPipe, FormDialog],
  templateUrl: './address-dialog.html',
})
export class AddressDialog {
  private readonly api = inject(ShopApi);
  private readonly ref = inject(MatDialogRef<AddressDialog, Address>);
  private readonly transloco = inject(TranslocoService);
  protected readonly address = inject<Address | null>(MAT_DIALOG_DATA);

  protected readonly fields = ['fullName', 'phone', 'city', 'area', 'street', 'building', 'notes'] as const;
  protected readonly form = inject(FormBuilder).nonNullable.group({
    fullName: [this.address?.fullName ?? '', [Validators.required, Validators.maxLength(200)]],
    phone: [this.address?.phone ?? '', [Validators.required, Validators.pattern(/^\+?[0-9 ]{7,20}$/)]],
    city: [this.address?.city ?? '', [Validators.required, Validators.maxLength(100)]],
    area: [this.address?.area ?? '', [Validators.required, Validators.maxLength(100)]],
    street: [this.address?.street ?? '', [Validators.required, Validators.maxLength(200)]],
    building: [this.address?.building ?? '', [Validators.required, Validators.maxLength(100)]],
    notes: [this.address?.notes ?? '', Validators.maxLength(500)],
    isDefault: [this.address?.isDefault ?? false],
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
    const request = { ...value, notes: value.notes.trim() || null };
    try {
      const saved = await firstValueFrom(this.address
        ? this.api.updateAddress(this.address.id, request)
        : this.api.createAddress(request));
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
