import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { CatalogApi } from '../../../../core/api/catalog-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { CartStore } from '../../../../core/cart/cart-store';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslocoService } from '@jsverse/transloco';
import { ProductVariant } from '../../../../core/api/models';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';

/** GET /products/{slug}: photos, options, prices and stock. */
@Component({
  selector: 'app-product-detail',
  imports: [
    RouterLink, CurrencyPipe, DecimalPipe, MatButton, MatIconButton, MatButtonToggleGroup, MatButtonToggle, MatIcon, TranslocoPipe,
    Loading, EmptyState,
  ],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
})
export class ProductDetailPage {
  private readonly api = inject(CatalogApi);
  private readonly router = inject(Router);
  private readonly cart = inject(CartStore);
  private readonly snackBar = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);
  protected readonly session = inject(AuthSession);
  protected readonly quantity = signal(1);
  protected readonly adding = signal(false);

  /** From the URL: /products/:slug */
  readonly slug = input.required<string>();

  protected readonly product = rxResource({
    params: () => this.slug(),
    stream: ({ params }) => this.api.product(params),
  });

  /** The chosen option; starts at the first one in stock (or the first one). */
  protected readonly variantId = linkedSignal(() => {
    const variants = this.product.value()?.variants ?? [];
    return (variants.find((v) => v.inStock) ?? variants[0])?.id;
  });
  protected readonly variant = computed<ProductVariant | undefined>(() =>
    this.product.value()?.variants.find((v) => v.id === this.variantId()));

  /** The photo shown large; starts at the main photo and resets when another product opens. */
  protected readonly imageUrl = linkedSignal<string | null>(() => this.product.value()?.images[0]?.url ?? null);
  protected readonly selectedImage = this.imageUrl;

  /** Quantity limits match the API (1–99 per line). */
  protected readonly maxQuantity = 99;

  protected showImage(url: string): void {
    this.imageUrl.set(url);
  }

  protected selectVariant(variantId: string): void {
    this.variantId.set(variantId);
  }

  protected decreaseQuantity(): void {
    this.quantity.update((q) => Math.max(1, q - 1));
  }

  protected increaseQuantity(): void {
    this.quantity.update((q) => Math.min(this.maxQuantity, q + 1));
  }

  protected async addToCart(): Promise<void> {
    const variant = this.variant();
    if (!variant) {
      return;
    }
    if (!this.session.isLoggedIn()) {
      await this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    this.adding.set(true);
    try {
      await this.cart.add(variant.id, this.quantity());
      this.snackBar
        .open(this.transloco.translate('store.added'), this.transloco.translate('store.viewCart'), { duration: 4000 })
        .onAction()
        .subscribe(() => void this.router.navigate(['/cart']));
    } catch {
      // e.g. "Not enough stock. Only 2 left." — shown by the error toast.
    } finally {
      this.adding.set(false);
    }
  }
}
