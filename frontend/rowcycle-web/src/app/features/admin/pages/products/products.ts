import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CatalogAdminApi } from '../../../../core/api/catalog-admin-api';
import { AdminProductListItem } from '../../../../core/api/models';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { FilterBar } from '../../../../shared/ui/filter-bar/filter-bar';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

/** /admin/products (StoreManager, Admin): every product, including inactive ones. */
@Component({
  selector: 'app-admin-products',
  imports: [
    RouterLink, CurrencyPipe, MatButton, MatIconButton, MatIcon, MatFormField, MatLabel, MatSelect,
    MatOption, MatPaginator, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState, FilterBar,
  ],
  templateUrl: './products.html',
  styleUrl: './products.scss',
})
export class AdminProducts {
  private readonly api = inject(CatalogAdminApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  protected readonly columns = ['image', 'name', 'price', 'stock', 'status', 'actions'];
  protected readonly search = signal('');
  protected readonly categoryId = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  protected readonly categories = rxResource({ stream: () => this.api.categories() });
  protected readonly products = rxResource({
    params: () => ({ search: this.search(), categoryId: this.categoryId(), page: this.page(), pageSize: this.pageSize() }),
    stream: ({ params }) => this.api.search(params),
  });

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  /** A new search starts again from the first page. */
  protected onSearch(text: string | null): void {
    this.search.set(text ?? '');
    this.page.set(1);
  }

  protected onCategoryChange(categoryId: string): void {
    this.categoryId.set(categoryId);
    this.page.set(1);
  }

  protected async remove(product: AdminProductListItem): Promise<void> {
    const message = this.transloco.translate('admin.catalog.products.deleteConfirm', { name: product.name });
    if (!(await confirm(this.dialog, { message }))) {
      return;
    }
    try {
      await firstValueFrom(this.api.delete(product.id));
      this.notifier.success('admin.catalog.products.deleted');
      this.products.reload();
    } catch {
      // Shown by the error toast.
    }
  }
}
