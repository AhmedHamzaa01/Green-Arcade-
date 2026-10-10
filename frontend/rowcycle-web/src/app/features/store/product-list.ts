import { Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatChipListbox, MatChipOption } from '@angular/material/chips';
import { MatFormField, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { CatalogApi } from '../../core/api/catalog-api';
import { ProductSort } from '../../core/api/api.models';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';
import { ProductCard } from './product-card';

/**
 * GET /products. The filters live in the URL (?category=medals&search=gold&sort=PriceAsc&page=2),
 * so a filtered list can be shared or bookmarked and the back button works.
 */
@Component({
  selector: 'app-product-list',
  imports: [
    MatChipListbox, MatChipOption, MatFormField, MatLabel, MatPrefix, MatSuffix, MatIcon, MatIconButton, MatInput,
    MatSelect, MatOption, MatPaginator, TranslocoPipe, PageHeader, Loading, EmptyState, ProductCard,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'store.title' | transloco" [subtitle]="'store.subtitle' | transloco" />

      <div class="filters">
        <mat-form-field class="search" subscriptSizing="dynamic">
          <mat-label>{{ 'store.search' | transloco }}</mat-label>
          <mat-icon matPrefix>search</mat-icon>
          <input #box matInput [value]="search() ?? ''" (keyup.enter)="go({ search: box.value || null, page: null })" />
          @if (search()) {
            <button matIconButton matSuffix (click)="box.value = ''; go({ search: null, page: null })"><mat-icon>close</mat-icon></button>
          }
        </mat-form-field>
        <mat-form-field class="sort" subscriptSizing="dynamic">
          <mat-label>{{ 'store.sort.label' | transloco }}</mat-label>
          <mat-select [value]="currentSort()" (valueChange)="go({ sort: $event, page: null })">
            @for (option of sorts; track option) {
              <mat-option [value]="option">{{ 'store.sort.' + option | transloco }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </div>

      @if (categories.value(); as list) {
        <mat-chip-listbox class="categories" [value]="category() ?? ''" (change)="go({ category: $event.value || null, page: null })">
          <mat-chip-option value="">{{ 'store.allCategories' | transloco }}</mat-chip-option>
          @for (c of list; track c.id) {
            <mat-chip-option [value]="c.slug">{{ c.name }}</mat-chip-option>
          }
        </mat-chip-listbox>
      }

      @if (products.value(); as data) {
        @if (data.total === 0) {
          <app-empty-state icon="search_off" [message]="'store.empty' | transloco" />
        } @else {
          <div class="grid">
            @for (product of data.items; track product.id) {
              <app-product-card [product]="product" />
            }
          </div>
          <mat-paginator
            [length]="data.total"
            [pageIndex]="currentPage() - 1"
            [pageSize]="pageSize"
            [hidePageSize]="true"
            (page)="onPage($event)" />
        }
      } @else if (products.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .filters { display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 8px; }
    .search { flex: 1 1 240px; }
    .sort { flex: 0 1 220px; }
    .categories { display: block; margin: 8px 0 16px; }
    .grid { display: grid; gap: 16px; grid-template-columns: repeat(2, 1fr); }
    @media (min-width: 720px) { .grid { grid-template-columns: repeat(3, 1fr); } }
    @media (min-width: 960px) { .grid { grid-template-columns: repeat(4, 1fr); } }
  `,
})
export class ProductList {
  private readonly api = inject(CatalogApi);
  private readonly router = inject(Router);

  // Query parameters (bound by the router).
  readonly category = input<string>();
  readonly search = input<string>();
  readonly sort = input<string>();
  readonly page = input<string>();

  protected readonly sorts: ProductSort[] = ['Newest', 'PriceAsc', 'PriceDesc'];
  protected readonly pageSize = 12;
  protected readonly currentSort = computed<ProductSort>(() =>
    this.sorts.includes(this.sort() as ProductSort) ? (this.sort() as ProductSort) : 'Newest');
  protected readonly currentPage = computed(() => Math.max(1, Number(this.page()) || 1));

  protected readonly categories = rxResource({ stream: () => this.api.categories() });

  protected readonly products = rxResource({
    params: () => ({
      category: this.category(),
      search: this.search(),
      sort: this.currentSort(),
      page: this.currentPage(),
      pageSize: this.pageSize,
    }),
    stream: ({ params }) => this.api.products(params),
  });

  /** Changes the URL; the inputs and the product list follow. */
  protected go(changes: Record<string, string | null>): void {
    void this.router.navigate([], { queryParams: changes, queryParamsHandling: 'merge' });
  }

  protected onPage(event: PageEvent): void {
    this.go({ page: event.pageIndex === 0 ? null : String(event.pageIndex + 1) });
    window.scrollTo({ top: 0 });
  }
}
