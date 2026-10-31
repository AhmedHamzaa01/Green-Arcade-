import { Component, effect, inject, input, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminUserDetail, Role } from '../../../../core/api/models';
import { UsersAdminApi } from '../../../../core/api/users-admin-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { Notifier } from '../../../../core/errors/notifier';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PointsLedger } from '../../../../shared/ui/points-ledger/points-ledger';
import { AdjustPointsDialog } from '../../components/adjust-points-dialog/adjust-points-dialog';
import { ALL_ROLES } from '../users/users';

/** /admin/users/:id (Admin): one user — roles, block/unblock, manual points and their history. Every change is audit-logged. */
@Component({
  selector: 'app-admin-user-detail',
  imports: [
    DecimalPipe, RouterLink, MatButton, MatCard, MatCardContent, MatCheckbox, MatIcon, TranslocoPipe, Loading, PointsLedger,
  ],
  templateUrl: './user-detail.html',
  styleUrl: './user-detail.scss',
})
export class AdminUserDetailPage {
  private readonly api = inject(UsersAdminApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);
  private readonly session = inject(AuthSession);

  readonly id = input.required<string>();
  protected readonly roles = ALL_ROLES;
  protected readonly user = rxResource({ params: () => this.id(), stream: ({ params }) => this.api.get(params) });
  protected readonly pointsPage = signal(1);
  protected readonly points = rxResource({
    params: () => ({ id: this.id(), page: this.pointsPage() }),
    stream: ({ params }) => this.api.points(params.id, params.page, 10),
  });
  protected readonly selected = signal<Role[]>([]);
  protected readonly busy = signal(false);

  constructor() {
    // The checkboxes start from the user's saved roles, and reset whenever the user is reloaded.
    effect(() => this.selected.set([...(this.user.value()?.roles ?? [])]));
  }

  protected toggleRole(role: Role, checked: boolean): void {
    this.selected.update((list) => (checked ? [...list, role] : list.filter((r) => r !== role)));
  }

  protected rolesChanged(user: AdminUserDetail): boolean {
    const now = [...this.selected()].sort().join();
    return now !== [...user.roles].sort().join();
  }

  protected async saveRoles(user: AdminUserDetail): Promise<void> {
    await this.run(async () => {
      this.user.set(await firstValueFrom(this.api.setRoles(user.id, this.selected())));
      this.notifier.success('admin.users.rolesSaved');
      // Editing your own roles: refresh the menu (the API already re-issued your access on the next call).
      if (user.id === this.session.user()?.id) {
        await this.session.reloadUser();
      }
    });
  }

  protected async toggleBlock(user: AdminUserDetail): Promise<void> {
    const key = user.isActive ? 'admin.users.blockConfirm' : 'admin.users.unblockConfirm';
    if (!(await confirm(this.dialog, { message: this.transloco.translate(key, { name: user.fullName }), confirmKey: user.isActive ? 'admin.users.block' : 'admin.users.unblock' }))) {
      return;
    }
    await this.run(async () => {
      this.user.set(await firstValueFrom(this.api.setBlocked(user.id, user.isActive)));
      this.notifier.success(user.isActive ? 'admin.users.blockedDone' : 'admin.users.unblockedDone');
    });
  }

  protected async adjust(user: AdminUserDetail): Promise<void> {
    const change = await firstValueFrom(this.dialog.open(AdjustPointsDialog, { data: user, width: '420px' }).afterClosed());
    if (change) {
      this.notifier.success('admin.users.adjusted');
      this.user.reload();
      this.pointsPage.set(1);
      this.points.reload();
    }
  }

  protected onPointsPage(change: { page: number }): void {
    this.pointsPage.set(change.page);
  }

  private async run(work: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    try {
      await work();
    } catch {
      this.user.reload();   // the error toast explains what went wrong
    } finally {
      this.busy.set(false);
    }
  }
}
