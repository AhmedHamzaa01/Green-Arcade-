import { Component, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Order, OrderStatus } from '../../../../core/api/models';
import { AdminOrdersApi } from '../../../../core/api/shop-api';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { Loading } from '../../../../shared/ui/loading/loading';
import { OrderView } from '../../../orders/components/order-view/order-view';

/** /admin/orders/:id — buttons only for the next allowed statuses (FR-16). */
@Component({
  selector: 'app-admin-order-detail',
  imports: [RouterLink, MatButton, MatCard, MatCardContent, MatIcon, TranslocoPipe, Loading, OrderView],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.scss',
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
