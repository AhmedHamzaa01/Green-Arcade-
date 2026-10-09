import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslocoService } from '@jsverse/transloco';

/** Short messages at the bottom of the screen. */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);

  success(key: string): void {
    this.snackBar.open(this.transloco.translate(key), undefined, { duration: 3000 });
  }

  /** Shows an already-translated message (e.g. the API's `title`) or a translation key. */
  error(messageOrKey: string): void {
    this.snackBar.open(this.transloco.translate(messageOrKey), this.transloco.translate('common.close'), {
      duration: 6000,
      panelClass: 'snack-error',
    });
  }
}
