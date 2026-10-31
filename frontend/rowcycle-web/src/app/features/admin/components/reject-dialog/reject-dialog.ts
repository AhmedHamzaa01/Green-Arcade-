import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe } from '@jsverse/transloco';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { FormDialog } from '../../../../shared/ui/form-dialog/form-dialog';

/** Asks for the reason the member will see. Closes with the reason, or nothing when cancelled. */
@Component({
  selector: 'app-reject-dialog',
  imports: [
    ReactiveFormsModule, MatFormField, MatLabel, MatHint, MatError, MatInput, TranslocoPipe, FieldErrorPipe, FormDialog,
  ],
  templateUrl: './reject-dialog.html',
  styleUrl: './reject-dialog.scss',
})
export class RejectDialog {
  private readonly ref = inject(MatDialogRef<RejectDialog, string>);
  protected readonly reason = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] });

  protected reject(): void {
    if (!this.reason.value.trim()) {
      this.reason.setErrors({ required: true });
    }
    if (this.reason.invalid) {
      this.reason.markAsTouched();
      return;
    }
    this.ref.close(this.reason.value.trim());
  }
}
