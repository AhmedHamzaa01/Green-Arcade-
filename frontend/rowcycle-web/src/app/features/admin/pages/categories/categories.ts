import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CatalogAdminApi } from '../../../../core/api/catalog-admin-api';
import { ProductCategory } from '../../../../core/api/models';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { CategoryDialog } from '../../components/category-dialog/category-dialog';

/** /admin/categories (StoreManager, Admin). */
@Component({
  selector: 'app-admin-categories',
  imports: [MatButton, MatIconButton, MatIcon, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
})
export class AdminCategories {
  private readonly api = inject(CatalogAdminApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  protected readonly columns = ['order', 'name', 'status', 'actions'];
  protected readonly categories = rxResource({ stream: () => this.api.categories() });

  protected async edit(category: ProductCategory | null): Promise<void> {
    const saved = await firstValueFrom(this.dialog.open(CategoryDialog, { data: category, width: '420px' }).afterClosed());
    if (saved) {
      this.notifier.success('admin.catalog.categories.saved');
      this.categories.reload();
    }
  }

  protected async remove(category: ProductCategory): Promise<void> {
    const message = this.transloco.translate('admin.catalog.categories.deleteConfirm', { name: category.name });
    if (!(await confirm(this.dialog, { message }))) {
      return;
    }
    try {
      await firstValueFrom(this.api.deleteCategory(category.id));
      this.notifier.success('admin.catalog.categories.deleted');
      this.categories.reload();
    } catch {
      // e.g. 409 "Category is not empty." — already shown by the error toast.
    }
  }
}
