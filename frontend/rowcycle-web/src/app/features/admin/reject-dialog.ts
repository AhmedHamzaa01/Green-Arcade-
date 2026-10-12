import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoPipe } from '@jsverse/transloco';
import { FieldErrorPipe } from '../../shared/field-error.pipe';

/** Asks for the reason the member will see. Closes with the reason, or nothing when cancelled. */
@Component({
  selector: 'app-reject-dialog',
  imports: [
    ReactiveFormsModule, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton,
    MatFormField, MatLabel, MatHint, MatError, MatInput, TranslocoPipe, FieldErrorPipe,
  ],
  template: `
    <h2 mat-dialog-title>{{ 'admin.submissions.rejectTitle' | transloco }}</h2>
    <form (ngSubmit)="reject()">
      <mat-dialog-content>
        <mat-form-field class="full">
          <mat-label>{{ 'admin.submissions.reason' | transloco }}</mat-label>
          <textarea matInput rows="3" [formControl]="reason" cdkFocusInitial></textarea>
          <mat-hint>{{ 'admin.submissions.reasonHint' | transloco }}</mat-hint>
          <mat-error>{{ reason | fieldError }}</mat-error>
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>{{ 'common.cancel' | transloco }}</button>
        <button mat-flat-button class="danger" type="submit">{{ 'admin.submissions.reject' | transloco }}</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .full { width: 100%; }
    .danger { --mat-button-filled-container-color: var(--mat-sys-error); --mat-button-filled-label-text-color: var(--mat-sys-on-error); }
  `,
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
