import { Component, input, output } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';

/**
 * The filter row above a list: an optional search box (Enter searches, × clears), any extra filters placed inside
 * (e.g. selects), and an optional row of chips marked with the `chips` attribute, which scrolls sideways on phones.
 *
 * ```html
 * <app-filter-bar [searchLabel]="'admin.users.search' | transloco" [searchValue]="search()" (searchChange)="search.set($event)">
 *   <mat-form-field>…a select…</mat-form-field>
 *   <mat-chip-listbox chips>…</mat-chip-listbox>
 * </app-filter-bar>
 * ```
 */
@Component({
  selector: 'app-filter-bar',
  imports: [MatIconButton, MatFormField, MatLabel, MatPrefix, MatSuffix, MatIcon, MatInput],
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.scss',
})
export class FilterBar {
  /** Label of the search box (already translated). No label, no search box. */
  readonly searchLabel = input<string>();
  /** The current search text, e.g. from the URL. */
  readonly searchValue = input<string | null>('');
  /** The new search text (trimmed), or null when cleared. */
  readonly searchChange = output<string | null>();

  protected submit(text: string): void {
    this.searchChange.emit(text.trim() || null);
  }

  /** Empties the box and shows everything again. */
  protected clear(box: HTMLInputElement): void {
    box.value = '';
    this.submit('');
  }
}
