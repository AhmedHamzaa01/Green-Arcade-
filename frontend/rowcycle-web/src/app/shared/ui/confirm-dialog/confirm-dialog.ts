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
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss',
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmData>(MAT_DIALOG_DATA);
}

/** Opens the dialog; resolves to true when confirmed. */
export async function confirm(dialog: MatDialog, data: ConfirmData): Promise<boolean> {
  return (await firstValueFrom(dialog.open(ConfirmDialog, { data, width: '400px' }).afterClosed())) === true;
}
