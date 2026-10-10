import { Component, input } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCard } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { ProductListItem } from '../../core/api/api.models';

/** One product in the store grid. */
@Component({
  selector: 'app-product-card',
  imports: [RouterLink, CurrencyPipe, DecimalPipe, MatCard, MatIcon, TranslocoPipe],
  template: `
    @let p = product();
    <a class="card-link" [routerLink]="['/products', p.slug]">
      <mat-card class="card" [class.sold-out]="!p.inStock">
        <div class="image">
          @if (p.imageUrl) {
            <img [src]="p.imageUrl" [alt]="p.name" loading="lazy" />
          } @else {
            <mat-icon>image</mat-icon>
          }
          @if (!p.inStock) {
            <span class="badge out">{{ 'store.outOfStock' | transloco }}</span>
          }
        </div>
        <div class="body">
          <span class="muted category">{{ p.categoryName }}</span>
          <span class="name">{{ p.name }}</span>
          <span class="price">{{ p.priceEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</span>
          @if (p.pricePoints) {
            <span class="muted">{{ 'store.orPoints' | transloco: { points: (p.pricePoints | number) } }}</span>
          }
          @if (p.rewardPoints > 0) {
            <span class="earn"><mat-icon>stars</mat-icon>{{ 'store.earnPoints' | transloco: { points: p.rewardPoints } }}</span>
          }
        </div>
      </mat-card>
    </a>
  `,
  styles: `
    .card-link { text-decoration: none; color: inherit; display: block; height: 100%; }
    .card { height: 100%; overflow: hidden; transition: box-shadow 0.15s; }
    .card:hover { box-shadow: var(--mat-sys-level3); }
    .image { position: relative; aspect-ratio: 1; background: var(--mat-sys-surface-container); display: grid; place-items: center; }
    .image img { width: 100%; height: 100%; object-fit: cover; }
    .image mat-icon { font-size: 48px; width: 48px; height: 48px; color: var(--mat-sys-outline); }
    .sold-out img { opacity: 0.5; }
    .badge { position: absolute; top: 8px; left: 8px; padding: 2px 8px; border-radius: 12px; font: var(--mat-sys-label-small); }
    .badge.out { background: var(--mat-sys-error-container); color: var(--mat-sys-on-error-container); }
    .body { display: flex; flex-direction: column; gap: 2px; padding: 12px; }
    .category { font: var(--mat-sys-label-small); text-transform: uppercase; }
    .name { font: var(--mat-sys-title-small); }
    .price { font: var(--mat-sys-title-medium); color: var(--mat-sys-primary); }
    .earn { display: flex; align-items: center; gap: 4px; font: var(--mat-sys-label-medium); color: var(--mat-sys-tertiary); }
    .earn mat-icon { font-size: 16px; width: 16px; height: 16px; }
  `,
})
export class ProductCard {
  readonly product = input.required<ProductListItem>();
}
