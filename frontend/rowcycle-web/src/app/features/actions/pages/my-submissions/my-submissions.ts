import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { SubmissionsApi } from '../../../../core/api/submissions-api';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SubmissionStatusChip } from '../../../../shared/ui/submission-status-chip/submission-status-chip';

/** /actions/mine — the member's submissions and what happened to them, newest first (F3). */
@Component({
  selector: 'app-my-submissions',
  imports: [DatePipe, RouterLink, MatButton, MatCard, MatCardContent, MatIcon, MatPaginator, TranslocoPipe, PageHeader, Loading, EmptyState, SubmissionStatusChip],
  templateUrl: './my-submissions.html',
  styleUrl: './my-submissions.scss',
})
export class MySubmissions {
  private readonly api = inject(SubmissionsApi);
  protected readonly page = signal(1);
  protected readonly submissions = rxResource({ params: () => this.page(), stream: ({ params }) => this.api.mine(params, 10) });

  protected onPage(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
  }
}
