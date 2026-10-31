import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminBadgesApi } from '../../../../core/api/badges-api';
import { Badge } from '../../../../core/api/models';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { BadgeDialog } from '../../components/badge-dialog/badge-dialog';

/** /admin/badges (Admin): the badge rules (F4). */
@Component({
  selector: 'app-admin-badges',
  imports: [MatButton, MatIconButton, MatIcon, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState],
  templateUrl: './badges.html',
  styleUrl: './badges.scss',
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
