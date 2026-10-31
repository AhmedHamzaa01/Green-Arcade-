import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatRadioButton, MatRadioGroup } from '@angular/material/radio';
import { MatStep, MatStepper, MatStepperNext, MatStepperPrevious } from '@angular/material/stepper';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Address, PaymentMethod } from '../../../../core/api/models';
import { ShopApi } from '../../../../core/api/shop-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { CartStore } from '../../../../core/cart/cart-store';
import { Notifier } from '../../../../core/errors/notifier';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { AddressDialog } from '../../../account/components/address-dialog/address-dialog';

/** /checkout — address → payment → review → place order (F6). The API re-checks everything when placing. */
@Component({
  selector: 'app-checkout',
  imports: [
    CurrencyPipe, DecimalPipe, RouterLink, MatButton, MatCard, MatCardContent, MatIcon, MatRadioGroup, MatRadioButton,
    MatStepper, MatStep, MatStepperNext, MatStepperPrevious, TranslocoPipe, PageHeader, Loading,
  ],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class Checkout {
  private readonly api = inject(ShopApi);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);
  protected readonly session = inject(AuthSession);
  protected readonly cart = inject(CartStore);

  protected readonly addresses = rxResource({ stream: () => this.api.addresses() });
  protected readonly addressId = signal<string | null>(null);
  /** The chosen address, or the default one until the member picks another. */
  protected readonly address = computed<Address | undefined>(() => {
    const list = this.addresses.value() ?? [];
    return list.find((a) => a.id === this.addressId()) ?? list.find((a) => a.isDefault);
  });

  protected readonly paymentMethod = signal<PaymentMethod | null>('Cod');
  protected readonly balance = computed(() => this.session.user()?.pointsBalance ?? 0);
  protected readonly canPayWithPoints = computed(() => {
    const c = this.cart.cart();
    return !!c?.canPayWithPoints && (c.totalPoints ?? 0) <= this.balance();
  });
  protected readonly placing = signal(false);

  constructor() {
    void this.cart.reload();
    void this.session.reloadUser();
  }

  protected selectAddress(addressId: string): void {
    this.addressId.set(addressId);
  }

  protected selectPayment(method: PaymentMethod): void {
    this.paymentMethod.set(method);
  }

  protected async addAddress(): Promise<void> {
    const saved = await firstValueFrom(this.dialog.open(AddressDialog, { data: null, width: '560px' }).afterClosed());
    if (saved) {
      this.addresses.reload();
      this.addressId.set(saved.id);
    }
  }

  protected async place(): Promise<void> {
    const address = this.address();
    const method = this.paymentMethod();
    if (!address || !method) {
      return;
    }

    this.placing.set(true);
    try {
      const order = await firstValueFrom(this.api.checkout(address.id, method));
      this.notifier.success(this.transloco.translate('checkout.placed', { number: order.orderNumber }));
      await Promise.all([this.cart.reload(), this.session.reloadUser()]);
      await this.router.navigate(['/account/orders', order.id]);
    } catch {
      // e.g. "Not enough stock. Only 1 left of …" — shown by the error toast; refresh so the cart shows the problem.
      await this.cart.reload();
    } finally {
      this.placing.set(false);
    }
  }
}
