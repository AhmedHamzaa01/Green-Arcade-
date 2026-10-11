import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { CatalogApi } from '../../core/api/catalog-api';
import { AuthSession } from '../../core/auth/auth-session';
import { CartStore } from '../../core/cart/cart-store';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslocoService } from '@jsverse/transloco';
import { ProductVariant } from '../../core/api/api.models';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';

/** GET /products/{slug}: photos, options, prices and stock. */
@Component({
  selector: 'app-product-detail',
  imports: [
    RouterLink, CurrencyPipe, DecimalPipe, MatButton, MatIconButton, MatButtonToggleGroup, MatButtonToggle, MatIcon, TranslocoPipe,
    Loading, EmptyState,
  ],
  template: `
    <div class="page">
      <a mat-button routerLink="/products"><mat-icon>arrow_back</mat-icon>{{ 'store.backToStore' | transloco }}</a>

      @if (product.value(); as p) {
        <div class="layout">
          <div class="gallery">
            <div class="main-image">
              @if (selectedImage(); as url) {
                <img [src]="url" [alt]="p.name" />
              } @else {
                <mat-icon>image</mat-icon>
              }
            </div>
            @if (p.images.length > 1) {
              <div class="thumbs">
                @for (image of p.images; track image.id) {
                  <button type="button" class="thumb" [class.selected]="image.url === selectedImage()" (click)="imageUrl.set(image.url)">
                    <img [src]="image.url" alt="" />
                  </button>
                }
              </div>
            }
          </div>

          <div class="info stack">
            <span class="muted category">{{ p.categoryName }}</span>
            <h1>{{ p.name }}</h1>

            <div class="price">{{ (variant()?.priceEgp ?? p.priceEgp) | currency: 'EGP' : 'symbol' : '1.0-2' }}</div>
            @if (p.pricePoints) {
              <div class="muted">{{ 'store.orPoints' | transloco: { points: (p.pricePoints | number) } }}</div>
            }
            @if (p.rewardPoints > 0) {
              <div class="earn"><mat-icon>stars</mat-icon>{{ 'store.earnPoints' | transloco: { points: p.rewardPoints } }}</div>
            }

            @if (p.variants.length > 1) {
              <div>
                <div class="muted">{{ 'store.variant' | transloco }}</div>
                <mat-button-toggle-group [value]="variant()?.id" (change)="variantId.set($event.value)">
                  @for (v of p.variants; track v.id) {
                    <mat-button-toggle [value]="v.id" [disabled]="!v.inStock">{{ v.name }}</mat-button-toggle>
                  }
                </mat-button-toggle-group>
              </div>
            }

            @if (variant()?.inStock) {
              <span class="row positive"><mat-icon>check_circle</mat-icon>{{ 'store.inStock' | transloco }}</span>
            } @else {
              <span class="row negative"><mat-icon>block</mat-icon>{{ 'store.outOfStock' | transloco }}</span>
            }

            @if (variant()?.inStock) {
              <div class="row">
                <div class="qty">
                  <button matIconButton (click)="quantity.set(quantity() - 1)" [disabled]="quantity() <= 1"><mat-icon>remove</mat-icon></button>
                  <span>{{ quantity() }}</span>
                  <button matIconButton (click)="quantity.set(quantity() + 1)" [disabled]="quantity() >= 99"><mat-icon>add</mat-icon></button>
                </div>
                <button mat-flat-button (click)="addToCart()" [disabled]="adding()">
                  <mat-icon>add_shopping_cart</mat-icon>
                  {{ (session.isLoggedIn() ? 'store.addToCart' : 'store.loginToBuy') | transloco }}
                </button>
              </div>
            }

            @if (p.description) {
              <p class="description">{{ p.description }}</p>
            }
          </div>
        </div>
      } @else if (product.isLoading()) {
        <app-loading />
      } @else if (product.error()) {
        <app-empty-state icon="search_off" [message]="'store.notFound' | transloco" />
      }
    </div>
  `,
  styles: `
    .layout { display: grid; gap: 24px; margin-top: 8px; }
    @media (min-width: 720px) { .layout { grid-template-columns: 1fr 1fr; } }
    .main-image { aspect-ratio: 1; border-radius: 12px; overflow: hidden; background: var(--mat-sys-surface-container); display: grid; place-items: center; }
    .main-image img { width: 100%; height: 100%; object-fit: cover; }
    .main-image mat-icon { font-size: 64px; width: 64px; height: 64px; color: var(--mat-sys-outline); }
    .thumbs { display: flex; gap: 8px; margin-top: 8px; flex-wrap: wrap; }
    .thumb { width: 64px; height: 64px; padding: 0; border: 2px solid transparent; border-radius: 8px; overflow: hidden; cursor: pointer; background: none; }
    .thumb.selected { border-color: var(--mat-sys-primary); }
    .thumb img { width: 100%; height: 100%; object-fit: cover; }
    .category { font: var(--mat-sys-label-medium); text-transform: uppercase; }
    h1 { font: var(--mat-sys-headline-small); margin: 0; }
    .price { font: var(--mat-sys-headline-medium); color: var(--mat-sys-primary); }
    .earn { display: flex; align-items: center; gap: 6px; color: var(--mat-sys-tertiary); font: var(--mat-sys-title-small); }
    .hint { font: var(--mat-sys-body-small); }
    .description { white-space: pre-line; }
    .qty { display: flex; align-items: center; gap: 4px; }
  `,
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
