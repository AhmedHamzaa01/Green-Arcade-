import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { PointsApi } from '../../../../core/api/points-api';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { PointsLedger } from '../../../../shared/ui/points-ledger/points-ledger';

/** GET /me/points: balance + ledger, newest first, paged (F2). */
@Component({
  selector: 'app-points-history',
  imports: [DecimalPipe, MatIcon, TranslocoPipe, PageHeader, Loading, EmptyState, PointsLedger],
  templateUrl: './points-history.html',
  styleUrl: './points-history.scss',
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

  protected onPage(change: { page: number; pageSize: number }): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }
}
