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
import { Submission } from '../../core/api/api.models';
import { ModerationApi } from '../../core/api/submissions-api';
import { Notifier } from '../../core/errors/notifier';
import { confirm } from '../../shared/confirm-dialog';
import { Loading } from '../../shared/loading';
import { SubmissionStatusChip } from '../../shared/submission-status-chip';
import { RejectDialog } from './reject-dialog';

/** /admin/submissions/:id — look at the photo, then approve (points go to the member) or reject with a reason. */
@Component({
  selector: 'app-submission-review',
  imports: [DatePipe, RouterLink, MatButton, MatCard, MatCardContent, MatIcon, TranslocoPipe, Loading, SubmissionStatusChip],
  template: `
    <div class="page">
      <a mat-button routerLink="/admin/submissions"><mat-icon>arrow_back</mat-icon>{{ 'admin.submissions.back' | transloco }}</a>
      @if (submission.value(); as s) {
        <div class="layout">
          <a class="photo" [href]="s.photoUrl" target="_blank" rel="noopener">
            <img [src]="s.photoUrl" [alt]="'admin.submissions.photoAlt' | transloco: { member: s.memberName }" />
          </a>
          <mat-card>
            <mat-card-content class="stack">
              <div class="row"><h2>{{ s.categoryName }}</h2><app-submission-status [status]="s.status" /></div>
              <dl>
                <dt>{{ 'admin.submissions.member' | transloco }}</dt><dd>{{ s.memberName }}</dd>
                <dt>{{ 'admin.submissions.sent' | transloco }}</dt><dd>{{ s.createdAt | date: 'medium' }}</dd>
                <dt>{{ 'admin.submissions.points' | transloco }}</dt><dd>{{ s.categoryPoints }}</dd>
                @if (s.weightKg !== null) {
                  <dt>{{ 'actions.new.weight' | transloco }}</dt><dd>{{ s.weightKg }} kg</dd>
                }
                @if (s.note) {
                  <dt>{{ 'actions.new.note' | transloco }}</dt><dd class="note">{{ s.note }}</dd>
                }
                @if (s.reviewedAt) {
                  <dt>{{ 'admin.submissions.reviewed' | transloco }}</dt><dd>{{ s.reviewedAt | date: 'medium' }}</dd>
                }
                @if (s.rejectReason) {
                  <dt>{{ 'admin.submissions.reason' | transloco }}</dt><dd class="note">{{ s.rejectReason }}</dd>
                }
              </dl>
              @if (s.status === 'Pending') {
                <div class="row">
                  <button mat-flat-button (click)="approve(s)" [disabled]="busy()">
                    <mat-icon>check</mat-icon>{{ 'admin.submissions.approve' | transloco }}
                  </button>
                  <button mat-stroked-button class="danger" (click)="reject(s)" [disabled]="busy()">
                    <mat-icon>close</mat-icon>{{ 'admin.submissions.reject' | transloco }}
                  </button>
                </div>
              }
            </mat-card-content>
          </mat-card>
        </div>
      } @else if (submission.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .layout { display: grid; gap: 16px; margin-top: 8px; grid-template-columns: 1fr; }
    @media (min-width: 840px) { .layout { grid-template-columns: 3fr 2fr; align-items: start; } }
    .photo img { width: 100%; max-height: 70vh; object-fit: contain; border-radius: 8px; background: var(--mat-sys-surface-container); }
    h2 { margin: 0; }
    dl { display: grid; grid-template-columns: max-content 1fr; gap: 6px 16px; margin: 0; }
    dt { color: var(--mat-sys-on-surface-variant); }
    dd { margin: 0; }
    .note { overflow-wrap: anywhere; white-space: pre-line; }
    .danger { --mat-button-outlined-label-text-color: var(--mat-sys-error); }
  `,
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
