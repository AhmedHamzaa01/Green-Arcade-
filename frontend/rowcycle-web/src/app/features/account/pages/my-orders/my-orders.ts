import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { ShopApi } from '../../../../core/api/shop-api';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { OrderStatusChip } from '../../../../shared/ui/order-status-chip/order-status-chip';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

/** /account/orders — the member's orders, newest first (F7). */
@Component({
  selector: 'app-my-orders',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, RouterLink, MatCard, MatCardContent, MatIcon, MatPaginator, TranslocoPipe, PageHeader, Loading, EmptyState, OrderStatusChip],
  templateUrl: './my-orders.html',
  styleUrl: './my-orders.scss',
})
export class MyOrders {
  private readonly api = inject(ShopApi);
  protected readonly page = signal(1);
  protected readonly orders = rxResource({ params: () => this.page(), stream: ({ params }) => this.api.myOrders(params, 10) });

  protected onPage(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
  }
}
