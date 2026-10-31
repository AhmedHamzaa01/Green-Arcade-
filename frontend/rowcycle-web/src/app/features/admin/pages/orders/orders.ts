import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatChipListbox, MatChipOption } from '@angular/material/chips';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe } from '@jsverse/transloco';
import { OrderStatus } from '../../../../core/api/models';
import { AdminOrdersApi } from '../../../../core/api/shop-api';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { FilterBar } from '../../../../shared/ui/filter-bar/filter-bar';
import { Loading } from '../../../../shared/ui/loading/loading';
import { OrderStatusChip } from '../../../../shared/ui/order-status-chip/order-status-chip';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

/** /admin/orders (StoreManager, Admin). */
@Component({
  selector: 'app-admin-orders',
  imports: [
    CurrencyPipe, DatePipe, DecimalPipe, RouterLink, MatChipListbox, MatChipOption,
    MatPaginator, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState, OrderStatusChip, FilterBar,
  ],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
})
export class AdminOrders {
  private readonly api = inject(AdminOrdersApi);

  protected readonly statuses: OrderStatus[] = ['Placed', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'];
  protected readonly columns = ['number', 'date', 'customer', 'total', 'status'];
  protected readonly status = signal<OrderStatus | ''>('');
  protected readonly search = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  protected readonly orders = rxResource({
    params: () => ({ status: this.status(), search: this.search(), page: this.page(), pageSize: this.pageSize() }),
    stream: ({ params }) => this.api.search(params),
  });

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  // Every filter change starts again from the first page.
  protected onSearch(text: string | null): void {
    this.search.set(text ?? '');
    this.page.set(1);
  }

  protected onStatusChange(status: OrderStatus | '' | null): void {
    this.status.set(status ?? '');
    this.page.set(1);
  }
}
