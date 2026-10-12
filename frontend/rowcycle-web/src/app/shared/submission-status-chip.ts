import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SubmissionStatus } from '../core/api/api.models';

/** A coloured label for a submission's status. */
@Component({
  selector: 'app-submission-status',
  imports: [TranslocoPipe],
  template: `<span class="chip" [class]="status().toLowerCase()">{{ 'submissionStatus.' + status() | transloco }}</span>`,
  styles: `
    .chip { display: inline-block; padding: 2px 10px; border-radius: 12px; font: var(--mat-sys-label-medium); white-space: nowrap; }
    .pending { background: var(--mat-sys-secondary-container); color: var(--mat-sys-on-secondary-container); }
    .approved { background: var(--mat-sys-primary-container); color: var(--mat-sys-on-primary-container); }
    .rejected { background: var(--mat-sys-error-container); color: var(--mat-sys-on-error-container); }
  `,
})
export class SubmissionStatusChip {
  readonly status = input.required<SubmissionStatus>();
}
