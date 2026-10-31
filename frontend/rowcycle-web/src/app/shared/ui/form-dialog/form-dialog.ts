import { Component, input } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from '@angular/material/dialog';
import { TranslocoPipe } from '@jsverse/transloco';

/**
 * The frame every form dialog shares: title, the fields, an error line, and Cancel / Save buttons.
 * Put it inside the dialog's own `<form [formGroup] (ngSubmit)>` so the fields stay bound to that form:
 *
 * ```html
 * <form [formGroup]="form" (ngSubmit)="save()">
 *   <app-form-dialog [title]="'x.title' | transloco" [error]="error()" [saving]="saving()">
 *     <mat-form-field>…</mat-form-field>
 *   </app-form-dialog>
 * </form>
 * ```
 */
@Component({
  selector: 'app-form-dialog',
  imports: [MatButton, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, TranslocoPipe],
  templateUrl: './form-dialog.html',
  styleUrl: './form-dialog.scss',
})
export class FormDialog {
  /** Already translated. */
  readonly title = input.required<string>();
  /** A message from the API that isn't about one field (already translated). */
  readonly error = input<string | null>(null);
  /** Disables the submit button while saving. */
  readonly saving = input(false);
  /** Translation key of the submit button. */
  readonly submitKey = input('common.save');
  /** Red submit button, for destructive actions (e.g. reject). */
  readonly danger = input(false);
  /** 'stack' (one column) or 'grid' (two columns on wide screens). */
  readonly layout = input<'stack' | 'grid'>('stack');
}
