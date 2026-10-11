import { Component, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Order, OrderStatus } from '../../core/api/api.models';
import { AdminOrdersApi } from '../../core/api/shop-api';
import { Notifier } from '../../core/errors/notifier';
import { confirm } from '../../shared/confirm-dialog';
import { Loading } from '../../shared/loading';
import { OrderView } from '../orders/order-view';

/** /admin/orders/:id — buttons only for the next allowed statuses (FR-16). */
@Component({
  selector: 'app-admin-order-detail',
  imports: [RouterLink, MatButton, MatCard, MatCardContent, MatIcon, TranslocoPipe, Loading, OrderView],
  template: `
    <div class="page">
      <a mat-button routerLink="/admin/orders"><mat-icon>arrow_back</mat-icon>{{ 'admin.orders.back' | transloco }}</a>
      @if (order.value(); as o) {
        <mat-card class="actions">
          <mat-card-content class="row">
            @if (o.nextStatuses.length === 0) {
              <span class="muted">{{ 'admin.orders.final' | transloco }}</span>
            } @else {
              <span class="muted">{{ 'admin.orders.moveTo' | transloco }}</span>
              @for (s of o.nextStatuses; track s) {
                <button mat-flat-button [class.danger]="s === 'Cancelled'" (click)="move(o, s)" [disabled]="busy()">
                  {{ 'status.' + s | transloco }}
                </button>
              }
            }
          </mat-card-content>
        </mat-card>
        <app-order-view [order]="o" />
      } @else if (order.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .actions { margin: 8px 0 16px; }
    .danger { --mat-button-filled-container-color: var(--mat-sys-error); --mat-button-filled-label-text-color: var(--mat-sys-on-error); }
  `,
})
export class AdminOrderDetail {
  private readonly api = inject(AdminOrdersApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  readonly id = input.required<string>();
  protected readonly order = rxResource({ params: () => this.id(), stream: ({ params }) => this.api.get(params) });
  protected readonly busy = signal(false);

  protected async move(order: Order, status: OrderStatus): Promise<void> {
    const statusName = this.transloco.translate(`status.${status}`);
    let message = this.transloco.translate('admin.orders.changeConfirm', { number: order.orderNumber, status: statusName });
    if (status === 'Delivered' && order.totalRewardPoints > 0) {
      message += ' ' + this.transloco.translate('admin.orders.deliverHint', { points: order.totalRewardPoints });
    } else if (status === 'Cancelled') {
      message += ' ' + this.transloco.translate('admin.orders.cancelHint');
    }
    if (!(await confirm(this.dialog, { message, confirmKey: `status.${status}` }))) {
      return;
    }

    this.busy.set(true);
    try {
      this.order.set(await firstValueFrom(this.api.changeStatus(order.id, status)));
      this.notifier.success('admin.orders.changed');
    } catch {
      this.order.reload();   // e.g. someone else changed it first; the toast explains
    } finally {
      this.busy.set(false);
    }
  }
}
