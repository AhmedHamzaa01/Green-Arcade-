import { Component, inject } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { CartItem } from '../../../../core/api/models';
import { CartStore } from '../../../../core/cart/cart-store';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

/** /cart — today's prices; problems (sold out, removed) are marked and block checkout. */
@Component({
  selector: 'app-cart',
  imports: [CurrencyPipe, DecimalPipe, RouterLink, MatButton, MatIconButton, MatCard, MatCardContent, MatIcon, TranslocoPipe, PageHeader, Loading, EmptyState],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class CartPage {
  protected readonly store = inject(CartStore);

  constructor() {
    // Fresh prices and stock every time the page opens.
    void this.store.reload();
  }

  protected async change(item: CartItem, step: number): Promise<void> {
    try {
      await this.store.setQuantity(item.variantId, item.quantity + step);
    } catch {
      // e.g. "Not enough stock. Only 2 left." — shown by the error toast.
    }
  }

  protected async remove(item: CartItem): Promise<void> {
    await this.store.remove(item.variantId);
  }
}
