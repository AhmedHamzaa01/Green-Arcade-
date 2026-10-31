import { Component, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Submission } from '../../../../core/api/models';
import { ModerationApi } from '../../../../core/api/submissions-api';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { Loading } from '../../../../shared/ui/loading/loading';
import { SubmissionStatusChip } from '../../../../shared/ui/submission-status-chip/submission-status-chip';
import { RejectDialog } from '../../components/reject-dialog/reject-dialog';

/** /admin/submissions/:id — look at the photo, then approve (points go to the member) or reject with a reason. */
@Component({
  selector: 'app-submission-review',
  imports: [DatePipe, RouterLink, MatButton, MatCard, MatCardContent, MatIcon, TranslocoPipe, Loading, SubmissionStatusChip],
  templateUrl: './submission-review.html',
  styleUrl: './submission-review.scss',
})
export class SubmissionReview {
  private readonly api = inject(ModerationApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  readonly id = input.required<string>();
  protected readonly submission = rxResource({ params: () => this.id(), stream: ({ params }) => this.api.get(params) });
  protected readonly busy = signal(false);

  protected async approve(s: Submission): Promise<void> {
    const message = this.transloco.translate('admin.submissions.approveConfirm', { points: s.categoryPoints, member: s.memberName });
    if (await confirm(this.dialog, { message, confirmKey: 'admin.submissions.approve' })) {
      await this.run(this.api.approve(s.id), 'admin.submissions.approved');
    }
  }

  protected async reject(s: Submission): Promise<void> {
    const reason = await firstValueFrom(this.dialog.open(RejectDialog, { width: '440px' }).afterClosed());
    if (reason) {
      await this.run(this.api.reject(s.id, reason), 'admin.submissions.rejected');
    }
  }

  private async run(call: Observable<Submission>, successKey: string): Promise<void> {
    this.busy.set(true);
    try {
      this.submission.set(await firstValueFrom(call));
      this.notifier.success(successKey);
    } catch {
      this.submission.reload();   // e.g. another moderator reviewed it first; the toast explains
    } finally {
      this.busy.set(false);
    }
  }
}
