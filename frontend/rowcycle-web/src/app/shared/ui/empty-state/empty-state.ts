import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

/** Shown when a list has nothing in it. Optionally offers the next step as a button. Pass already-translated text. */
@Component({
  selector: 'app-empty-state',
  imports: [RouterLink, MatButton, MatIcon],
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss',
})
export class EmptyState {
  readonly message = input.required<string>();
  readonly icon = input('inbox');
  readonly actionLabel = input<string>();
  readonly actionLink = input<string>();
}
