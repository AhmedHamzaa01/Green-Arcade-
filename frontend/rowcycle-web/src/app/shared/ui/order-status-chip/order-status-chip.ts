import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { OrderStatus } from '../../../core/api/models';
import { StatusChip, StatusTone } from '../status-chip/status-chip';

const TONES: Record<OrderStatus, StatusTone> = {
  Placed: 'warning',
  Confirmed: 'info',
  Shipped: 'info',
  Delivered: 'success',
  Cancelled: 'danger',
};

/** A coloured label for an order's status. */
@Component({
  selector: 'app-order-status',
  imports: [TranslocoPipe, StatusChip],
  templateUrl: './order-status-chip.html',
})
export class OrderStatusChip {
  readonly status = input.required<OrderStatus>();
  protected readonly tone = computed(() => TONES[this.status()]);
}
