import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminBadgesApi } from '../../core/api/badges-api';
import { Badge } from '../../core/api/api.models';
import { Notifier } from '../../core/errors/notifier';
import { confirm } from '../../shared/confirm-dialog';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';
import { BadgeDialog } from './badge-dialog';

/** /admin/badges (Admin): the badge rules (F4). */
@Component({
  selector: 'app-admin-badges',
  imports: [MatButton, MatIconButton, MatIcon, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.badges.title' | transloco" [subtitle]="'admin.badges.subtitle' | transloco">
        <button mat-flat-button class="new" (click)="edit(null)"><mat-icon>add</mat-icon>{{ 'admin.badges.new' | transloco }}</button>
      </app-page-header>

      @if (badges.value(); as list) {
        @if (list.length === 0) {
          <app-empty-state icon="emoji_events" [message]="'admin.badges.empty' | transloco" />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="list">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.badges.name' | transloco }}</th>
                <td mat-cell *matCellDef="let b"><span class="row"><mat-icon class="badge-icon">{{ b.icon }}</mat-icon>{{ b.name }}</span></td>
              </ng-container>
              <ng-container matColumnDef="rule">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.badges.rule' | transloco }}</th>
                <td mat-cell *matCellDef="let b">{{ describe(b) }}</td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let b" class="actions">
                  <button matIconButton (click)="edit(b)" [attr.aria-label]="'common.edit' | transloco"><mat-icon>edit</mat-icon></button>
                  <button matIconButton (click)="remove(b)" [attr.aria-label]="'common.delete' | transloco"><mat-icon>delete</mat-icon></button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
        }
      } @else if (badges.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .new { margin-top: 12px; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    .badge-icon { color: var(--mat-sys-primary); }
    .actions { text-align: right; white-space: nowrap; }
  `,
})
export class AdminBadges {
  private readonly api = inject(AdminBadgesApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  protected readonly columns = ['name', 'rule', 'actions'];
  protected readonly badges = rxResource({ stream: () => this.api.list() });

  /** e.g. "5 approved actions in Can collection". */
  protected describe(badge: Badge): string {
    return this.transloco.translate(`admin.badges.describe.${badge.ruleType}`, {
      count: badge.threshold,
      category: badge.ruleCategoryName,
    });
  }

  protected async edit(badge: Badge | null): Promise<void> {
    const saved = await firstValueFrom(this.dialog.open(BadgeDialog, { data: badge, width: '480px' }).afterClosed());
    if (saved) {
      this.notifier.success('admin.badges.saved');
      this.badges.reload();
    }
  }

  protected async remove(badge: Badge): Promise<void> {
    const message = this.transloco.translate('admin.badges.deleteConfirm', { name: badge.name });
    if (!(await confirm(this.dialog, { message }))) {
      return;
    }
    try {
      await firstValueFrom(this.api.delete(badge.id));
      this.notifier.success('admin.badges.deleted');
      this.badges.reload();
    } catch {
      // Already shown by the error toast.
    }
  }
}
