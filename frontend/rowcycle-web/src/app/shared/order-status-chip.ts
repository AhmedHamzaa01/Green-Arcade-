import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { OrderStatus } from '../core/api/api.models';

/** A coloured label for an order's status. */
@Component({
  selector: 'app-order-status',
  imports: [TranslocoPipe],
  template: `<span class="chip" [class]="status().toLowerCase()">{{ 'status.' + status() | transloco }}</span>`,
  styles: `
    .chip { display: inline-block; padding: 2px 10px; border-radius: 12px; font: var(--mat-sys-label-medium); white-space: nowrap;
            background: var(--mat-sys-surface-container-high); color: var(--mat-sys-on-surface); }
    .placed { background: var(--mat-sys-secondary-container); color: var(--mat-sys-on-secondary-container); }
    .confirmed, .shipped { background: var(--mat-sys-tertiary-container); color: var(--mat-sys-on-tertiary-container); }
    .delivered { background: var(--mat-sys-primary-container); color: var(--mat-sys-on-primary-container); }
    .cancelled { background: var(--mat-sys-error-container); color: var(--mat-sys-on-error-container); }
  `,
})
export class OrderStatusChip {
  readonly status = input.required<OrderStatus>();
}
