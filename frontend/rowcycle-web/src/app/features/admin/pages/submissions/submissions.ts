import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatChipListbox, MatChipOption } from '@angular/material/chips';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { SubmissionStatus } from '../../../../core/api/models';
import { ModerationApi } from '../../../../core/api/submissions-api';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { FilterBar } from '../../../../shared/ui/filter-bar/filter-bar';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SubmissionStatusChip } from '../../../../shared/ui/submission-status-chip/submission-status-chip';

/** /admin/submissions (Moderator, Admin): the queue, oldest first; Pending by default. */
@Component({
  selector: 'app-admin-submissions',
  imports: [
    DatePipe, RouterLink, MatCard, MatCardContent, MatChipListbox, MatChipOption, MatPaginator, TranslocoPipe,
    PageHeader, Loading, EmptyState, SubmissionStatusChip, FilterBar,
  ],
  templateUrl: './submissions.html',
  styleUrl: './submissions.scss',
})
export class AdminSubmissions {
  private readonly api = inject(ModerationApi);

  protected readonly statuses: SubmissionStatus[] = ['Pending', 'Approved', 'Rejected'];
  protected readonly status = signal<SubmissionStatus | ''>('Pending');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(12);

  protected readonly submissions = rxResource({
    params: () => ({ status: this.status(), page: this.page(), pageSize: this.pageSize() }),
    stream: ({ params }) => this.api.search(params),
  });

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  /** A different status starts again from the first page. */
  protected onStatusChange(status: SubmissionStatus | '' | null): void {
    this.status.set(status ?? '');
    this.page.set(1);
  }
}
