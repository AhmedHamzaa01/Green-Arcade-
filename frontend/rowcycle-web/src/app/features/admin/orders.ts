import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatChipListbox, MatChipOption } from '@angular/material/chips';
import { MatFormField, MatLabel, MatPrefix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe } from '@jsverse/transloco';
import { OrderStatus } from '../../core/api/api.models';
import { AdminOrdersApi } from '../../core/api/shop-api';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { OrderStatusChip } from '../../shared/order-status-chip';
import { PageHeader } from '../../shared/page-header';

/** /admin/orders (StoreManager, Admin). */
@Component({
  selector: 'app-admin-orders',
  imports: [
    CurrencyPipe, DatePipe, DecimalPipe, RouterLink, MatChipListbox, MatChipOption, MatFormField, MatLabel, MatPrefix, MatIcon,
    MatInput, MatPaginator, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState, OrderStatusChip,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.orders.title' | transloco" [subtitle]="'admin.orders.subtitle' | transloco" />

      <mat-chip-listbox [value]="status()" (change)="status.set($event.value ?? ''); page.set(1)">
        <mat-chip-option value="">{{ 'common.all' | transloco }}</mat-chip-option>
        @for (s of statuses; track s) {
          <mat-chip-option [value]="s">{{ 'status.' + s | transloco }}</mat-chip-option>
        }
      </mat-chip-listbox>
      <mat-form-field class="search" subscriptSizing="dynamic">
        <mat-label>{{ 'admin.orders.search' | transloco }}</mat-label>
        <mat-icon matPrefix>search</mat-icon>
        <input #box matInput (keyup.enter)="search.set(box.value); page.set(1)" />
      </mat-form-field>

      @if (orders.value(); as data) {
        @if (data.total === 0) {
          <app-empty-state icon="receipt_long" [message]="'admin.orders.empty' | transloco" />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="data.items">
              <ng-container matColumnDef="number">
                <th mat-header-cell *matHeaderCellDef>#</th>
                <td mat-cell *matCellDef="let o"><a [routerLink]="['/admin/orders', o.id]">{{ o.orderNumber }}</a></td>
              </ng-container>
              <ng-container matColumnDef="date">
                <th mat-header-cell *matHeaderCellDef>{{ 'orders.date' | transloco }}</th>
                <td mat-cell *matCellDef="let o">{{ o.createdAt | date: 'short' }}</td>
              </ng-container>
              <ng-container matColumnDef="customer">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.orders.customer' | transloco }}</th>
                <td mat-cell *matCellDef="let o">{{ o.recipientName }}<div class="muted small">{{ o.city }}</div></td>
              </ng-container>
              <ng-container matColumnDef="total">
                <th mat-header-cell *matHeaderCellDef>{{ 'orders.total' | transloco }}</th>
                <td mat-cell *matCellDef="let o">
                  @if (o.paymentMethod === 'Points') { {{ 'orders.paidPoints' | transloco: { points: (o.totalPoints | number) } }} }
                  @else { {{ o.totalEgp | currency: 'EGP' : 'symbol' : '1.0-2' }} }
                  <div class="muted small">{{ 'payment.' + o.paymentMethod | transloco }}</div>
                </td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let o"><app-order-status [status]="o.status" /></td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
          <mat-paginator [length]="data.total" [pageIndex]="page() - 1" [pageSize]="pageSize()" [pageSizeOptions]="[10, 20, 50]" (page)="onPage($event)" />
        }
      } @else if (orders.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .search { width: 100%; max-width: 360px; margin: 12px 0; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    .small { font-size: 12px; }
  `,
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
}
