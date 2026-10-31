import { Component, input, output } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { PointsEntry } from '../../../core/api/models';

/**
 * A page of points-ledger entries (Earn / Redeem / Reverse / Adjust) with a paginator.
 * Used on the member's Points page and on the admin's user page. The parent loads the data and handles paging.
 */
@Component({
  selector: 'app-points-ledger',
  imports: [DatePipe, DecimalPipe, MatIcon, MatPaginator, TranslocoPipe],
  templateUrl: './points-ledger.html',
  styleUrl: './points-ledger.scss',
})
export class PointsLedger {
  readonly entries = input.required<PointsEntry[]>();
  readonly total = input.required<number>();
  /** 1-based, like the API. */
  readonly page = input.required<number>();
  readonly pageSize = input(20);
  /** Offer 10/20/50 per page; otherwise the page size is fixed. */
  readonly choosePageSize = input(false);

  /** New 1-based page and page size. */
  readonly pageChange = output<{ page: number; pageSize: number }>();

  protected onPage(event: PageEvent): void {
    this.pageChange.emit({ page: event.pageIndex + 1, pageSize: event.pageSize });
  }
}
