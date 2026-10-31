import { Component, input } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';
import { Order } from '../../../../core/api/models';
import { OrderStatusChip } from '../../../../shared/ui/order-status-chip/order-status-chip';

/** Everything about one order: items, totals, address, payment and status history. Used by members and staff. */
@Component({
  selector: 'app-order-view',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, MatCard, MatCardContent, MatIcon, TranslocoPipe, OrderStatusChip],
  templateUrl: './order-view.html',
  styleUrl: './order-view.scss',
})
export class OrderView {
  readonly order = input.required<Order>();
}
