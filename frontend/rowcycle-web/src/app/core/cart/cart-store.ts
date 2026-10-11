import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Cart } from '../api/api.models';
import { ShopApi } from '../api/shop-api';
import { AuthSession } from '../auth/auth-session';

/** The signed-in member's cart, shared by the top bar, product pages, cart and checkout. */
@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly api = inject(ShopApi);
  private readonly session = inject(AuthSession);

  readonly cart = signal<Cart | null>(null);
  /** Number of items, for the badge on the cart icon. */
  readonly count = computed(() => this.cart()?.itemCount ?? 0);

  constructor() {
    // Load the cart on login, forget it on logout.
    effect(() => {
      if (this.session.isLoggedIn()) {
        void this.reload();
      } else {
        this.cart.set(null);
      }
    });
  }

  async reload(): Promise<void> {
    this.cart.set(await firstValueFrom(this.api.cart()));
  }

  async add(variantId: string, quantity: number): Promise<void> {
    this.cart.set(await firstValueFrom(this.api.addToCart(variantId, quantity)));
  }

  async setQuantity(variantId: string, quantity: number): Promise<void> {
    this.cart.set(await firstValueFrom(this.api.updateCartItem(variantId, quantity)));
  }

  async remove(variantId: string): Promise<void> {
    this.cart.set(await firstValueFrom(this.api.removeCartItem(variantId)));
  }
}
