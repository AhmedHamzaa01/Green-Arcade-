import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogActions, MatDialogClose, MatDialogContent } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { firstValueFrom } from 'rxjs';
import { TranslocoPipe } from '@jsverse/transloco';

export interface ConfirmData {
  /** Already translated. */
  message: string;
  confirmKey?: string;
}

/** "Are you sure?" before deleting something. */
@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogContent, MatDialogActions, MatDialogClose, MatButton, TranslocoPipe],
  template: `
    <mat-dialog-content><p>{{ data.message }}</p></mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ 'common.cancel' | transloco }}</button>
      <button mat-flat-button class="danger" [mat-dialog-close]="true">{{ (data.confirmKey ?? 'common.delete') | transloco }}</button>
    </mat-dialog-actions>
  `,
  styles: `.danger { --mat-button-filled-container-color: var(--mat-sys-error); --mat-button-filled-label-text-color: var(--mat-sys-on-error); }`,
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmData>(MAT_DIALOG_DATA);
}

/** Opens the dialog; resolves to true when confirmed. */
export async function confirm(dialog: MatDialog, data: ConfirmData): Promise<boolean> {
  return (await firstValueFrom(dialog.open(ConfirmDialog, { data, width: '400px' }).afterClosed())) === true;
}
