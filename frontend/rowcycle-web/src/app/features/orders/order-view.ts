import { Component, input } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { Order } from '../../core/api/api.models';
import { OrderStatusChip } from '../../shared/order-status-chip';

/** Everything about one order: items, totals, address, payment and status history. Used by members and staff. */
@Component({
  selector: 'app-order-view',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, MatCard, MatCardContent, MatIcon, TranslocoPipe, OrderStatusChip],
  template: `
    @let o = order();
    <div class="head row">
      <h1>{{ o.orderNumber }}</h1>
      <app-order-status [status]="o.status" />
      <span class="muted">{{ o.createdAt | date: 'medium' }}</span>
    </div>

    @if (o.rewardPending) {
      <p class="reward"><mat-icon>stars</mat-icon>{{ 'orders.rewardPending' | transloco: { points: o.totalRewardPoints } }}</p>
    } @else if (o.status === 'Delivered' && o.totalRewardPoints > 0) {
      <p class="reward"><mat-icon>stars</mat-icon>{{ 'orders.rewardEarned' | transloco: { points: o.totalRewardPoints } }}</p>
    }

    <div class="grid">
      <mat-card class="items">
        <mat-card-content>
          <h2>{{ 'orders.items' | transloco }}</h2>
          @for (item of o.items; track item.id) {
            <div class="line">
              @if (item.imageUrl) { <img [src]="item.imageUrl" alt="" /> } @else { <mat-icon class="muted">image</mat-icon> }
              <div class="grow">
                <div>{{ item.productName }}</div>
                <div class="muted small">{{ item.variantName }} · {{ item.quantity }} × {{ item.unitPriceEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</div>
              </div>
              <strong>{{ item.lineTotalEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</strong>
            </div>
          }
          <div class="line total">
            <span class="grow">{{ 'checkout.total' | transloco }}</span>
            @if (o.paymentMethod === 'Points') {
              <strong>{{ 'orders.paidPoints' | transloco: { points: (o.totalPoints | number) } }}</strong>
            } @else {
              <strong>{{ o.totalEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</strong>
            }
          </div>
        </mat-card-content>
      </mat-card>

      <div class="side">
        <mat-card>
          <mat-card-content>
            <h2>{{ 'orders.address' | transloco }}</h2>
            @let a = o.shippingAddress;
            <div>{{ a.fullName }} · {{ a.phone }}</div>
            <div class="muted">{{ a.street }}, {{ a.building }} — {{ a.area }}, {{ a.city }}</div>
            @if (a.notes) { <div class="muted small">{{ a.notes }}</div> }
            <h2 class="spaced">{{ 'orders.payment' | transloco }}</h2>
            <div>{{ 'payment.' + o.paymentMethod | transloco }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-content>
            <h2>{{ 'orders.history' | transloco }}</h2>
            <ol class="timeline">
              @for (step of o.statusHistory; track step.changedAt) {
                <li><app-order-status [status]="step.toStatus" /> <span class="muted small">{{ step.changedAt | date: 'medium' }}</span></li>
              }
            </ol>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: `
    .head { gap: 12px; margin: 8px 0; }
    h1 { font: var(--mat-sys-headline-small); margin: 0; }
    h2 { font: var(--mat-sys-title-small); margin: 0 0 8px; }
    .spaced { margin-top: 16px; }
    .reward { display: flex; align-items: center; gap: 6px; color: var(--mat-sys-tertiary); }
    .grid { display: grid; gap: 16px; }
    @media (min-width: 840px) { .grid { grid-template-columns: 3fr 2fr; } }
    .side { display: flex; flex-direction: column; gap: 16px; }
    .line { display: flex; align-items: center; gap: 12px; padding: 8px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .line img { width: 48px; height: 48px; object-fit: cover; border-radius: 8px; }
    .line.total { border-bottom: none; font: var(--mat-sys-title-medium); }
    .grow { flex: 1; }
    .small { font-size: 12px; }
    .timeline { margin: 0; padding-left: 18px; display: flex; flex-direction: column; gap: 8px; }
  `,
})
export class OrderView {
  readonly order = input.required<Order>();
}
