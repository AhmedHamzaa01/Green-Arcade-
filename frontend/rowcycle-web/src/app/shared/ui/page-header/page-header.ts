import { Component, input } from '@angular/core';

/**
 * Page title with an optional line underneath. Pass already-translated text.
 * Anything placed inside (buttons, links) shows on the right on wide screens and below the title on phones.
 */
@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.html',
  styleUrl: './page-header.scss',
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
