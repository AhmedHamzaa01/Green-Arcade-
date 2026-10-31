import { Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatChipListbox, MatChipOption } from '@angular/material/chips';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe } from '@jsverse/transloco';
import { CatalogApi } from '../../../../core/api/catalog-api';
import { ProductSort } from '../../../../core/api/models';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { FilterBar } from '../../../../shared/ui/filter-bar/filter-bar';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { ProductCard } from '../../components/product-card/product-card';

/**
 * GET /products. The filters live in the URL (?category=medals&search=gold&sort=PriceAsc&page=2),
 * so a filtered list can be shared or bookmarked and the back button works.
 */
@Component({
  selector: 'app-product-list',
  imports: [
    MatChipListbox, MatChipOption, MatFormField, MatLabel,
    MatSelect, MatOption, MatPaginator, TranslocoPipe, PageHeader, Loading, EmptyState, ProductCard, FilterBar,
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
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
