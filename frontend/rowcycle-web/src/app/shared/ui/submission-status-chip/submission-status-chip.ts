import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SubmissionStatus } from '../../../core/api/models';
import { StatusChip, StatusTone } from '../status-chip/status-chip';

const TONES: Record<SubmissionStatus, StatusTone> = {
  Pending: 'warning',
  Approved: 'success',
  Rejected: 'danger',
};

/** A coloured label for a submission's status. */
@Component({
  selector: 'app-submission-status',
  imports: [TranslocoPipe, StatusChip],
  templateUrl: './submission-status-chip.html',
})
export class SubmissionStatusChip {
  readonly status = input.required<SubmissionStatus>();
  protected readonly tone = computed(() => TONES[this.status()]);
}
