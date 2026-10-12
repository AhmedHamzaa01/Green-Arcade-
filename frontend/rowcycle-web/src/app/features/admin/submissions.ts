import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatChipListbox, MatChipOption } from '@angular/material/chips';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { SubmissionStatus } from '../../core/api/api.models';
import { ModerationApi } from '../../core/api/submissions-api';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';
import { SubmissionStatusChip } from '../../shared/submission-status-chip';

/** /admin/submissions (Moderator, Admin): the queue, oldest first; Pending by default. */
@Component({
  selector: 'app-admin-submissions',
  imports: [
    DatePipe, RouterLink, MatCard, MatCardContent, MatChipListbox, MatChipOption, MatPaginator, TranslocoPipe,
    PageHeader, Loading, EmptyState, SubmissionStatusChip,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.submissions.title' | transloco" [subtitle]="'admin.submissions.subtitle' | transloco" />

      <mat-chip-listbox [value]="status()" (change)="status.set($event.value ?? ''); page.set(1)">
        @for (s of statuses; track s) {
          <mat-chip-option [value]="s">{{ 'submissionStatus.' + s | transloco }}</mat-chip-option>
        }
        <mat-chip-option value="">{{ 'common.all' | transloco }}</mat-chip-option>
      </mat-chip-listbox>

      @if (submissions.value(); as data) {
        @if (data.total === 0) {
          <app-empty-state icon="task_alt" [message]="'admin.submissions.empty' | transloco" />
        } @else {
          <div class="grid">
            @for (s of data.items; track s.id) {
              <a class="card-link" [routerLink]="['/admin/submissions', s.id]">
                <mat-card>
                  <img [src]="s.photoUrl" alt="" loading="lazy" />
                  <mat-card-content>
                    <div class="row between"><strong>{{ s.categoryName }}</strong><app-submission-status [status]="s.status" /></div>
                    <div class="muted small">{{ s.memberName }} · {{ s.createdAt | date: 'short' }}</div>
                  </mat-card-content>
                </mat-card>
              </a>
            }
          </div>
          <mat-paginator [length]="data.total" [pageIndex]="page() - 1" [pageSize]="pageSize()" [pageSizeOptions]="[12, 24, 48]" (page)="onPage($event)" />
        }
      } @else if (submissions.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    mat-chip-listbox { display: block; margin-bottom: 16px; }
    .grid { display: grid; gap: 12px; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); }
    .card-link { text-decoration: none; color: inherit; }
    mat-card { overflow: hidden; height: 100%; }
    img { width: 100%; aspect-ratio: 4 / 3; object-fit: cover; }
    mat-card-content { padding-top: 8px; }
    .between { justify-content: space-between; }
    .small { font-size: 12px; }
  `,
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
}
