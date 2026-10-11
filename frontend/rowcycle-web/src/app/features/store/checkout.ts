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
import { Address, PaymentMethod } from '../../core/api/api.models';
import { ShopApi } from '../../core/api/shop-api';
import { AuthSession } from '../../core/auth/auth-session';
import { CartStore } from '../../core/cart/cart-store';
import { Notifier } from '../../core/errors/notifier';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';
import { AddressDialog } from '../account/address-dialog';

/** /checkout — address → payment → review → place order (F6). The API re-checks everything when placing. */
@Component({
  selector: 'app-checkout',
  imports: [
    CurrencyPipe, DecimalPipe, RouterLink, MatButton, MatCard, MatCardContent, MatIcon, MatRadioGroup, MatRadioButton,
    MatStepper, MatStep, MatStepperNext, MatStepperPrevious, TranslocoPipe, PageHeader, Loading,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'checkout.title' | transloco" />

      @if (!session.user()?.emailVerified) {
        <mat-card><mat-card-content class="row negative"><mat-icon>warning</mat-icon>{{ 'checkout.unverified' | transloco }}</mat-card-content></mat-card>
      } @else if (cart.cart(); as c) {
        @if (c.items.length === 0) {
          <p>{{ 'cart.empty' | transloco }} <a routerLink="/products">{{ 'cart.browse' | transloco }}</a></p>
        } @else {
          <mat-stepper linear #stepper orientation="vertical">
            <!-- 1. Address -->
            <mat-step [completed]="!!address()" [label]="'checkout.steps.address' | transloco">
              @if (addresses.value(); as list) {
                @if (list.length === 0) {
                  <p class="muted">{{ 'checkout.noAddresses' | transloco }}</p>
                }
                <mat-radio-group class="stack" [value]="addressId()" (change)="addressId.set($event.value)">
                  @for (a of list; track a.id) {
                    <mat-radio-button [value]="a.id">
                      <strong>{{ a.fullName }}</strong> · {{ a.phone }}<br />
                      <span class="muted">{{ a.street }}, {{ a.building }} — {{ a.area }}, {{ a.city }}</span>
                    </mat-radio-button>
                  }
                </mat-radio-group>
              }
              <div class="row actions">
                <button mat-stroked-button (click)="addAddress()"><mat-icon>add</mat-icon>{{ 'checkout.addAddress' | transloco }}</button>
                <button mat-flat-button matStepperNext [disabled]="!address()">{{ 'common.next' | transloco }}</button>
              </div>
            </mat-step>

            <!-- 2. Payment -->
            <mat-step [completed]="!!paymentMethod()" [label]="'checkout.steps.payment' | transloco">
              <mat-radio-group class="stack" [value]="paymentMethod()" (change)="paymentMethod.set($event.value)">
                <mat-radio-button value="Cod">
                  <strong>{{ 'payment.Cod' | transloco }}</strong><br />
                  <span class="muted">{{ 'checkout.codHint' | transloco }}</span>
                </mat-radio-button>
                <mat-radio-button value="Points" [disabled]="!canPayWithPoints()">
                  <strong>{{ 'payment.Points' | transloco }}</strong><br />
                  <span class="muted">
                    @if (!c.canPayWithPoints) {
                      {{ 'checkout.pointsNoPrice' | transloco }}
                    } @else if (!canPayWithPoints()) {
                      {{ 'checkout.pointsNotEnough' | transloco: { points: (c.totalPoints | number), balance: (balance() | number) } }}
                    } @else {
                      {{ 'checkout.pointsHint' | transloco: { points: (c.totalPoints | number), balance: (balance() | number) } }}
                    }
                  </span>
                </mat-radio-button>
              </mat-radio-group>
              <div class="row actions">
                <button mat-button matStepperPrevious>{{ 'common.back' | transloco }}</button>
                <button mat-flat-button matStepperNext [disabled]="!paymentMethod()">{{ 'common.next' | transloco }}</button>
              </div>
            </mat-step>

            <!-- 3. Review and place -->
            <mat-step [label]="'checkout.steps.review' | transloco">
              <div class="review stack">
                <div>
                  <h3>{{ 'checkout.items' | transloco }}</h3>
                  @for (item of c.items; track item.variantId) {
                    <div class="row between"><span>{{ item.quantity }} × {{ item.productName }} ({{ item.variantName }})</span>
                      <span>{{ item.lineTotalEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</span></div>
                  }
                  <div class="muted small">{{ 'checkout.free' | transloco }}</div>
                </div>
                @if (address(); as a) {
                  <div><h3>{{ 'checkout.deliverTo' | transloco }}</h3>{{ a.fullName }} · {{ a.phone }}<br /><span class="muted">{{ a.street }}, {{ a.building }} — {{ a.area }}, {{ a.city }}</span></div>
                }
                <div><h3>{{ 'checkout.payWith' | transloco }}</h3>{{ 'payment.' + paymentMethod() | transloco }}</div>
                <div class="row between total">
                  <span>{{ 'checkout.total' | transloco }}</span>
                  @if (paymentMethod() === 'Points') {
                    <strong>{{ 'orders.paidPoints' | transloco: { points: (c.totalPoints | number) } }}</strong>
                  } @else {
                    <strong>{{ c.totalEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}</strong>
                  }
                </div>
                @if (c.totalRewardPoints > 0) {
                  <div class="earn"><mat-icon>stars</mat-icon>{{ 'cart.earn' | transloco: { points: c.totalRewardPoints } }}</div>
                }
              </div>
              <div class="row actions">
                <button mat-button matStepperPrevious>{{ 'common.back' | transloco }}</button>
                <button mat-flat-button (click)="place()" [disabled]="placing() || c.hasIssues">{{ 'checkout.confirm' | transloco }}</button>
              </div>
            </mat-step>
          </mat-stepper>
        }
      } @else {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .actions { margin-top: 16px; }
    .review h3 { font: var(--mat-sys-title-small); margin: 0 0 4px; }
    .between { justify-content: space-between; }
    .total { font: var(--mat-sys-title-medium); border-top: 1px solid var(--mat-sys-outline-variant); padding-top: 8px; }
    .earn { display: flex; align-items: center; gap: 6px; color: var(--mat-sys-tertiary); }
    .small { font-size: 12px; }
    mat-radio-button { margin-bottom: 8px; }
  `,
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
