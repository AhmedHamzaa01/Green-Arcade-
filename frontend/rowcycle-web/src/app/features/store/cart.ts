import { Component, inject } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { CartItem } from '../../core/api/api.models';
import { CartStore } from '../../core/cart/cart-store';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

/** /cart — today's prices; problems (sold out, removed) are marked and block checkout. */
@Component({
  selector: 'app-cart',
  imports: [CurrencyPipe, DecimalPipe, RouterLink, MatButton, MatIconButton, MatCard, MatCardContent, MatIcon, TranslocoPipe, PageHeader, Loading, EmptyState],
  template: `
    <div class="page">
      <app-page-header [title]="'cart.title' | transloco" />
      @if (store.cart(); as cart) {
        @if (cart.items.length === 0) {
          <app-empty-state icon="shopping_cart" [message]="'cart.empty' | transloco" />
          <div class="center"><a mat-flat-button routerLink="/products">{{ 'cart.browse' | transloco }}</a></div>
        } @else {
          <div class="layout">
            <mat-card>
              <mat-card-content>
                @for (item of cart.items; track item.variantId) {
                  <div class="line" [class.problem]="!item.isAvailable || !item.hasEnoughStock">
                    <a [routerLink]="['/products', item.productSlug]" class="thumb">
                      @if (item.imageUrl) { <img [src]="item.imageUrl" alt="" /> } @else { <mat-icon class="muted">image</mat-icon> }
                    </a>
                    <div class="grow">
                      <a [routerLink]="['/products', item.productSlug]">{{ item.productName }}</a>
                      <div class="muted small">{{ item.variantName }} · {{ item.unitPriceEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</div>
                      @if (!item.isAvailable) {
                        <div class="negative small">{{ 'cart.unavailable' | transloco }}</div>
                      } @else if (!item.hasEnoughStock) {
                        <div class="negative small">{{ 'cart.notEnoughStock' | transloco }}</div>
                      }
                      <div class="qty">
                        <button matIconButton (click)="change(item, -1)" [disabled]="item.quantity <= 1"><mat-icon>remove</mat-icon></button>
                        <span>{{ item.quantity }}</span>
                        <button matIconButton (click)="change(item, 1)" [disabled]="!item.isAvailable"><mat-icon>add</mat-icon></button>
                        <button mat-button (click)="remove(item)">{{ 'cart.remove' | transloco }}</button>
                      </div>
                    </div>
                    <strong>{{ item.lineTotalEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</strong>
                  </div>
                }
              </mat-card-content>
            </mat-card>

            <mat-card class="summary">
              <mat-card-content class="stack">
                <div class="row between"><span>{{ 'cart.total' | transloco }}</span><strong class="total">{{ cart.totalEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</strong></div>
                @if (cart.totalPoints) {
                  <div class="muted">{{ 'cart.orPoints' | transloco: { points: (cart.totalPoints | number) } }}</div>
                }
                @if (cart.totalRewardPoints > 0) {
                  <div class="earn"><mat-icon>stars</mat-icon>{{ 'cart.earn' | transloco: { points: cart.totalRewardPoints } }}</div>
                }
                @if (cart.hasIssues) {
                  <div class="negative small">{{ 'cart.fixIssues' | transloco }}</div>
                }
                <a mat-flat-button routerLink="/checkout" [disabled]="cart.hasIssues">{{ 'cart.checkout' | transloco }}</a>
              </mat-card-content>
            </mat-card>
          </div>
        }
      } @else {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .layout { display: grid; gap: 16px; }
    @media (min-width: 840px) { .layout { grid-template-columns: 2fr 1fr; align-items: start; } }
    .line { display: flex; gap: 12px; padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .line:last-child { border-bottom: none; }
    .line.problem { background: color-mix(in srgb, var(--mat-sys-error-container) 30%, transparent); }
    .thumb { width: 64px; height: 64px; flex: none; display: grid; place-items: center; background: var(--mat-sys-surface-container); border-radius: 8px; overflow: hidden; }
    .thumb img { width: 100%; height: 100%; object-fit: cover; }
    .grow { flex: 1; min-width: 0; }
    .qty { display: flex; align-items: center; gap: 4px; margin-top: 4px; }
    .small { font-size: 12px; }
    .between { justify-content: space-between; }
    .total { font: var(--mat-sys-title-large); color: var(--mat-sys-primary); }
    .earn { display: flex; align-items: center; gap: 6px; color: var(--mat-sys-tertiary); }
    .center { text-align: center; }
  `,
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
