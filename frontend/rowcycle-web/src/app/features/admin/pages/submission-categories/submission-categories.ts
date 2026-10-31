import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminSubmissionCategory } from '../../../../core/api/models';
import { SubmissionCategoriesAdminApi } from '../../../../core/api/submissions-api';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SubmissionCategoryDialog } from '../../components/submission-category-dialog/submission-category-dialog';

/** /admin/submission-categories (Admin): what members can submit and what each pays. */
@Component({
  selector: 'app-admin-submission-categories',
  imports: [MatButton, MatIconButton, MatIcon, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState],
  templateUrl: './submission-categories.html',
  styleUrl: './submission-categories.scss',
})
export class AdminSubmissionCategories {
  private readonly api = inject(SubmissionCategoriesAdminApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  protected readonly columns = ['name', 'points', 'status', 'actions'];
  protected readonly categories = rxResource({ stream: () => this.api.list() });

  protected async edit(category: AdminSubmissionCategory | null): Promise<void> {
    const saved = await firstValueFrom(this.dialog.open(SubmissionCategoryDialog, { data: category, width: '420px' }).afterClosed());
    if (saved) {
      this.notifier.success('admin.actionCategories.saved');
      this.categories.reload();
    }
  }

  protected async remove(category: AdminSubmissionCategory): Promise<void> {
    const message = this.transloco.translate('admin.actionCategories.deleteConfirm', { name: category.name });
    if (!(await confirm(this.dialog, { message }))) {
      return;
    }
    try {
      await firstValueFrom(this.api.delete(category.id));
      this.notifier.success('admin.actionCategories.deleted');
      this.categories.reload();
    } catch {
      // e.g. 409 "Category is used by a badge." — already shown by the error toast.
    }
  }
}
