import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { SubmissionsApi } from '../../core/api/submissions-api';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';
import { SubmissionStatusChip } from '../../shared/submission-status-chip';

/** /actions/mine — the member's submissions and what happened to them, newest first (F3). */
@Component({
  selector: 'app-my-submissions',
  imports: [DatePipe, RouterLink, MatButton, MatCard, MatCardContent, MatIcon, MatPaginator, TranslocoPipe, PageHeader, Loading, EmptyState, SubmissionStatusChip],
  template: `
    <div class="page">
      <app-page-header [title]="'actions.mine.title' | transloco">
        <a mat-flat-button routerLink="/actions/new"><mat-icon>add_a_photo</mat-icon>{{ 'nav.newAction' | transloco }}</a>
      </app-page-header>
      @if (submissions.value(); as data) {
        @if (data.total === 0) {
          <app-empty-state icon="photo_camera" [message]="'actions.mine.empty' | transloco" />
        } @else {
          <div class="stack">
            @for (s of data.items; track s.id) {
              <mat-card>
                <mat-card-content class="item">
                  <img [src]="s.photoUrl" alt="" loading="lazy" />
                  <div class="details">
                    <div class="row"><strong>{{ s.categoryName }}</strong><app-submission-status [status]="s.status" /></div>
                    <div class="muted small">
                      {{ s.createdAt | date: 'medium' }}
                      @if (s.weightKg !== null) { · {{ s.weightKg }} kg }
                    </div>
                    @if (s.note) {
                      <p class="note">{{ s.note }}</p>
                    }
                    @switch (s.status) {
                      @case ('Approved') { <p class="positive">{{ 'actions.mine.earned' | transloco: { points: s.categoryPoints } }}</p> }
                      @case ('Rejected') { <p class="negative">{{ 'actions.mine.rejected' | transloco: { reason: s.rejectReason } }}</p> }
                      @default { <p class="muted">{{ 'actions.mine.waiting' | transloco }}</p> }
                    }
                  </div>
                </mat-card-content>
              </mat-card>
            }
          </div>
          <mat-paginator [length]="data.total" [pageIndex]="page() - 1" [pageSize]="10" [hidePageSize]="true" (page)="onPage($event)" />
        }
      } @else if (submissions.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .item { display: flex; gap: 12px; align-items: flex-start; }
    .item img { width: 88px; height: 88px; object-fit: cover; border-radius: 8px; flex: none; }
    .details { min-width: 0; }
    .details p { margin: 4px 0 0; }
    .note { overflow-wrap: anywhere; }
    .small { font-size: 12px; }
  `,
})
export class MySubmissions {
  private readonly api = inject(SubmissionsApi);
  protected readonly page = signal(1);
  protected readonly submissions = rxResource({ params: () => this.page(), stream: ({ params }) => this.api.mine(params, 10) });

  protected onPage(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
  }
}
