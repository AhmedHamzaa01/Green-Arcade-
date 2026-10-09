import { Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatList, MatListItem, MatListItemIcon, MatListItemLine, MatListItemTitle } from '@angular/material/list';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { PointsApi } from '../../core/api/points-api';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

/** GET /me/points: balance + ledger, newest first, paged (F2). */
@Component({
  selector: 'app-points-history',
  imports: [
    DatePipe, DecimalPipe, MatCard, MatCardContent, MatIcon, MatList, MatListItem, MatListItemIcon, MatListItemTitle,
    MatListItemLine, MatPaginator, TranslocoPipe, PageHeader, Loading, EmptyState,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'account.points.title' | transloco" />

      @if (history.value(); as data) {
        <mat-card>
          <mat-card-content>
            <div class="muted">{{ 'account.points.balance' | transloco }}</div>
            <div class="balance">{{ data.balance | number }}</div>
          </mat-card-content>
        </mat-card>

        @if (data.total === 0) {
          <app-empty-state icon="stars" [message]="'account.points.empty' | transloco" />
        } @else {
          <mat-list>
            @for (entry of data.items; track entry.id) {
              <mat-list-item>
                <mat-icon matListItemIcon [class]="entry.amount > 0 ? 'positive' : 'negative'">
                  {{ entry.amount > 0 ? 'add_circle' : 'remove_circle' }}
                </mat-icon>
                <span matListItemTitle class="row title">
                  <span>{{ 'account.points.types.' + entry.type | transloco }}</span>
                  <strong [class]="entry.amount > 0 ? 'positive' : 'negative'">
                    {{ entry.amount > 0 ? '+' : '' }}{{ entry.amount | number }}
                  </strong>
                </span>
                <span matListItemLine class="muted">
                  {{ 'account.points.sources.' + entry.sourceType | transloco }}
                  @if (entry.reason) { · {{ entry.reason }} }
                  · {{ entry.createdAt | date: 'medium' }}
                </span>
              </mat-list-item>
            }
          </mat-list>
          <mat-paginator
            [length]="data.total"
            [pageIndex]="page() - 1"
            [pageSize]="pageSize()"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="onPage($event)" />
        }
      } @else if (history.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .balance { font: var(--mat-sys-display-small); color: var(--mat-sys-primary); }
    .title { justify-content: space-between; width: 100%; }
  `,
})
export class PointsHistory {
  private readonly api = inject(PointsApi);

  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  /** Reloads automatically whenever page or pageSize changes. */
  protected readonly history = rxResource({
    params: () => ({ page: this.page(), pageSize: this.pageSize() }),
    stream: ({ params }) => this.api.history(params.page, params.pageSize),
  });

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }
}
