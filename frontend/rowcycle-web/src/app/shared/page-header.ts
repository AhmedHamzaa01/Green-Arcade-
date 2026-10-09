import { Component, input } from '@angular/core';

/** Page title with an optional line underneath. Pass already-translated text. */
@Component({
  selector: 'app-page-header',
  template: `
    <header class="page-header">
      <h1>{{ title() }}</h1>
      @if (subtitle()) {
        <p class="muted">{{ subtitle() }}</p>
      }
      <ng-content />
    </header>
  `,
  styles: `
    .page-header { margin: 8px 0 16px; }
    h1 { font: var(--mat-sys-headline-small); margin: 0; }
    p { margin: 4px 0 0; }
  `,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
