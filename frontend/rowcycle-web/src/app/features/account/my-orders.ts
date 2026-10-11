import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { ShopApi } from '../../core/api/shop-api';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { OrderStatusChip } from '../../shared/order-status-chip';
import { PageHeader } from '../../shared/page-header';

/** /account/orders — the member's orders, newest first (F7). */
@Component({
  selector: 'app-my-orders',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, RouterLink, MatCard, MatCardContent, MatIcon, MatPaginator, TranslocoPipe, PageHeader, Loading, EmptyState, OrderStatusChip],
  template: `
    <div class="page">
      <app-page-header [title]="'orders.title' | transloco" />
      @if (orders.value(); as data) {
        @if (data.total === 0) {
          <app-empty-state icon="receipt_long" [message]="'orders.empty' | transloco" />
        } @else {
          <div class="stack">
            @for (o of data.items; track o.id) {
              <a class="card-link" [routerLink]="['/account/orders', o.id]">
                <mat-card>
                  <mat-card-content class="row between">
                    <div>
                      <div class="row"><strong>{{ o.orderNumber }}</strong><app-order-status [status]="o.status" /></div>
                      <div class="muted small">{{ o.createdAt | date: 'mediumDate' }} · {{ 'orders.itemCount' | transloco: { count: o.itemCount } }}</div>
                    </div>
                    <div class="row">
                      @if (o.paymentMethod === 'Points') {
                        <strong>{{ 'orders.paidPoints' | transloco: { points: (o.totalPoints | number) } }}</strong>
                      } @else {
                        <strong>{{ o.totalEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</strong>
                      }
                      <mat-icon class="muted">chevron_right</mat-icon>
                    </div>
                  </mat-card-content>
                </mat-card>
              </a>
            }
          </div>
          <mat-paginator [length]="data.total" [pageIndex]="page() - 1" [pageSize]="10" [hidePageSize]="true" (page)="onPage($event)" />
        }
      } @else if (orders.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .card-link { text-decoration: none; color: inherit; }
    .between { justify-content: space-between; }
    .small { font-size: 12px; }
  `,
})
export class MyOrders {
  private readonly api = inject(ShopApi);
  protected readonly page = signal(1);
  protected readonly orders = rxResource({ params: () => this.page(), stream: ({ params }) => this.api.myOrders(params, 10) });

  protected onPage(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
  }
}
