import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ShopApi } from '../../../../core/api/shop-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { Loading } from '../../../../shared/ui/loading/loading';
import { OrderView } from '../../../orders/components/order-view/order-view';

/** /account/orders/:id — one order, with Cancel while it's still Placed. */
@Component({
  selector: 'app-my-order',
  imports: [RouterLink, MatButton, MatIcon, TranslocoPipe, Loading, OrderView],
  templateUrl: './my-order.html',
  styleUrl: './my-order.scss',
})
export class MyOrder {
  private readonly api = inject(ShopApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);
  private readonly session = inject(AuthSession);

  readonly id = input.required<string>();
  protected readonly order = rxResource({ params: () => this.id(), stream: ({ params }) => this.api.myOrder(params) });

  protected async cancel(id: string, number: string): Promise<void> {
    const message = this.transloco.translate('orders.cancelConfirm', { number });
    if (!(await confirm(this.dialog, { message, confirmKey: 'orders.cancelConfirmButton' }))) {
      return;
    }
    try {
      this.order.set(await firstValueFrom(this.api.cancelMyOrder(id)));
      this.notifier.success('orders.cancelled');
      await this.session.reloadUser();   // points may have come back
    } catch {
      // Shown by the error toast (e.g. it was confirmed meanwhile).
      this.order.reload();
    }
  }
}
