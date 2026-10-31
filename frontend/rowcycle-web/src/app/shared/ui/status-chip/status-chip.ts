import { Component, input } from '@angular/core';

export type StatusTone = 'neutral' | 'info' | 'warning' | 'success' | 'danger';

/** A small coloured label. Pass already-translated text and a tone. Order and submission chips build on this. */
@Component({
  selector: 'app-status-chip',
  templateUrl: './status-chip.html',
  styleUrl: './status-chip.scss',
})
export class StatusChip {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('neutral');
}
