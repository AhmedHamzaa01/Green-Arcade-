import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Address } from '../../core/api/api.models';
import { ShopApi } from '../../core/api/shop-api';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { FieldErrorPipe } from '../../shared/field-error.pipe';

/** Add (no data) or edit an address. Closes with the saved address. Used on the Addresses page and in checkout. */
@Component({
  selector: 'app-address-dialog',
  imports: [
    ReactiveFormsModule, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, MatCheckbox,
    MatFormField, MatLabel, MatError, MatInput, TranslocoPipe, FieldErrorPipe,
  ],
  template: `
    <h2 mat-dialog-title>{{ (address ? 'addresses.edit' : 'addresses.add') | transloco }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="grid">
        @for (field of fields; track field) {
          <mat-form-field [class.wide]="field === 'notes' || field === 'street'">
            <mat-label>{{ 'addresses.' + field | transloco }}</mat-label>
            <input matInput [formControlName]="field" [type]="field === 'phone' ? 'tel' : 'text'" />
            <mat-error>{{ form.get(field) | fieldError }}</mat-error>
          </mat-form-field>
        }
        <mat-checkbox class="wide" formControlName="isDefault">{{ 'addresses.makeDefault' | transloco }}</mat-checkbox>
        @if (error()) {
          <p class="form-error wide" role="alert">{{ error() }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>{{ 'common.cancel' | transloco }}</button>
        <button mat-flat-button type="submit" [disabled]="saving()">{{ 'common.save' | transloco }}</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .grid { display: grid; gap: 0 12px; grid-template-columns: 1fr; }
    @media (min-width: 600px) { .grid { grid-template-columns: 1fr 1fr; } .wide { grid-column: 1 / -1; } }
  `,
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
