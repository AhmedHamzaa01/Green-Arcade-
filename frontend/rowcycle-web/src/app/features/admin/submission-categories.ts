import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminSubmissionCategory } from '../../core/api/api.models';
import { SubmissionCategoriesAdminApi } from '../../core/api/submissions-api';
import { Notifier } from '../../core/errors/notifier';
import { confirm } from '../../shared/confirm-dialog';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';
import { SubmissionCategoryDialog } from './submission-category-dialog';

/** /admin/submission-categories (Admin): what members can submit and what each pays. */
@Component({
  selector: 'app-admin-submission-categories',
  imports: [MatButton, MatIconButton, MatIcon, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.actionCategories.title' | transloco" [subtitle]="'admin.actionCategories.subtitle' | transloco">
        <button mat-flat-button class="new" (click)="edit(null)"><mat-icon>add</mat-icon>{{ 'admin.actionCategories.new' | transloco }}</button>
      </app-page-header>

      @if (categories.value(); as list) {
        @if (list.length === 0) {
          <app-empty-state icon="category" [message]="'admin.actionCategories.empty' | transloco" />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="list">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.actionCategories.name' | transloco }}</th>
                <td mat-cell *matCellDef="let c">
                  {{ c.name }}
                  @if (c.tracksWeight) { <div class="muted small">{{ 'admin.actionCategories.tracksWeightShort' | transloco }}</div> }
                </td>
              </ng-container>
              <ng-container matColumnDef="points">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.actionCategories.points' | transloco }}</th>
                <td mat-cell *matCellDef="let c">{{ c.pointsReward }}</td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.catalog.products.status' | transloco }}</th>
                <td mat-cell *matCellDef="let c" [class]="c.isActive ? 'positive' : 'muted'">
                  {{ (c.isActive ? 'common.active' : 'common.inactive') | transloco }}
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let c" class="actions">
                  <button matIconButton (click)="edit(c)" [attr.aria-label]="'common.edit' | transloco"><mat-icon>edit</mat-icon></button>
                  <button matIconButton (click)="remove(c)" [attr.aria-label]="'common.delete' | transloco"><mat-icon>delete</mat-icon></button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
        }
      } @else if (categories.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .new { margin-top: 12px; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    .small { font-size: 12px; }
    .actions { text-align: right; white-space: nowrap; }
  `,
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
