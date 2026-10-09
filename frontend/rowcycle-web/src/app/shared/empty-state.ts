import { Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

/** Shown when a list has nothing in it. */
@Component({
  selector: 'app-empty-state',
  imports: [MatIcon],
  template: `
    <div class="empty">
      <mat-icon>{{ icon() }}</mat-icon>
      <p>{{ message() }}</p>
    </div>
  `,
  styles: `
    .empty { text-align: center; padding: 32px 16px; color: var(--mat-sys-on-surface-variant); }
    mat-icon { font-size: 40px; width: 40px; height: 40px; }
  `,
})
export class EmptyState {
  readonly message = input.required<string>();
  readonly icon = input('inbox');
}
