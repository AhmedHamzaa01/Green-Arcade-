import { Component, effect, inject, input, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminUserDetail, Role } from '../../core/api/api.models';
import { UsersAdminApi } from '../../core/api/users-admin-api';
import { AuthSession } from '../../core/auth/auth-session';
import { Notifier } from '../../core/errors/notifier';
import { confirm } from '../../shared/confirm-dialog';
import { Loading } from '../../shared/loading';
import { AdjustPointsDialog } from './adjust-points-dialog';
import { ALL_ROLES } from './users';

/** /admin/users/:id (Admin): one user — roles, block/unblock, manual points and their history. Every change is audit-logged. */
@Component({
  selector: 'app-admin-user-detail',
  imports: [
    DatePipe, DecimalPipe, RouterLink, MatButton, MatCard, MatCardContent, MatCheckbox, MatIcon, MatPaginator, TranslocoPipe, Loading,
  ],
  template: `
    <div class="page">
      <a mat-button routerLink="/admin/users"><mat-icon>arrow_back</mat-icon>{{ 'admin.users.back' | transloco }}</a>
      @if (user.value(); as u) {
        <div class="grid">
          <mat-card>
            <mat-card-content class="stack">
              <div>
                <h2>{{ u.fullName }}</h2>
                <div class="muted">{{ u.email }}@if (u.phone) { · {{ u.phone }} }</div>
              </div>
              <div class="row">
                <span [class]="u.isActive ? 'positive' : 'negative'">
                  {{ (u.isActive ? 'admin.users.active' : 'admin.users.blocked') | transloco }}
                </span>
                @if (!u.emailVerified) { <span class="muted">· {{ 'admin.users.unverified' | transloco }}</span> }
              </div>
              <dl>
                <dt>{{ 'admin.users.points' | transloco }}</dt><dd>{{ u.pointsBalance | number }}</dd>
                <dt>{{ 'admin.users.approved' | transloco }}</dt><dd>{{ u.approvedSubmissions }}</dd>
                <dt>{{ 'admin.users.pending' | transloco }}</dt><dd>{{ u.pendingSubmissions }}</dd>
                <dt>{{ 'admin.users.orders' | transloco }}</dt><dd>{{ u.orders }}</dd>
                <dt>{{ 'admin.users.badges' | transloco }}</dt><dd>{{ u.badges }}</dd>
              </dl>
              <div class="row actions">
                <button mat-stroked-button (click)="adjust(u)" [disabled]="busy()"><mat-icon>tune</mat-icon>{{ 'admin.users.adjust' | transloco }}</button>
                <button mat-stroked-button [class.danger]="u.isActive" (click)="toggleBlock(u)" [disabled]="busy()">
                  <mat-icon>{{ u.isActive ? 'block' : 'lock_open' }}</mat-icon>{{ (u.isActive ? 'admin.users.block' : 'admin.users.unblock') | transloco }}
                </button>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card>
            <mat-card-content class="stack">
              <div class="muted">{{ 'admin.users.roles' | transloco }}</div>
              @for (r of roles; track r) {
                <mat-checkbox [checked]="selected().includes(r)" (change)="toggleRole(r, $event.checked)">
                  {{ 'roles.' + r | transloco }}
                  <div class="muted small">{{ 'admin.users.roleHints.' + r | transloco }}</div>
                </mat-checkbox>
              }
              @if (selected().length === 0) { <p class="form-error">{{ 'admin.users.oneRole' | transloco }}</p> }
              <div class="row">
                <button mat-flat-button (click)="saveRoles(u)" [disabled]="busy() || selected().length === 0 || !rolesChanged(u)">
                  {{ 'admin.users.saveRoles' | transloco }}
                </button>
              </div>
              <p class="muted small">{{ 'admin.users.rolesHint' | transloco }}</p>
            </mat-card-content>
          </mat-card>
        </div>

        <h3 class="section">{{ 'admin.users.history' | transloco }}</h3>
        @if (points.value(); as h) {
          @if (h.total === 0) {
            <p class="muted">{{ 'admin.users.noHistory' | transloco }}</p>
          } @else {
            <mat-card>
              <mat-card-content>
                @for (e of h.items; track e.id) {
                  <div class="entry">
                    <div>
                      <div>{{ 'account.points.types.' + e.type | transloco }} · {{ 'account.points.sources.' + e.sourceType | transloco }}</div>
                      <div class="muted small">@if (e.reason) { {{ e.reason }} · }{{ e.createdAt | date: 'medium' }}</div>
                    </div>
                    <strong [class]="e.amount > 0 ? 'positive' : 'negative'">{{ e.amount > 0 ? '+' : '' }}{{ e.amount | number }}</strong>
                  </div>
                }
              </mat-card-content>
            </mat-card>
            <mat-paginator [length]="h.total" [pageIndex]="pointsPage() - 1" [pageSize]="10" [hidePageSize]="true" (page)="onPointsPage($event)" />
          }
        }
      } @else if (user.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .grid { display: grid; gap: 16px; margin-top: 8px; }
    @media (min-width: 840px) { .grid { grid-template-columns: 3fr 2fr; align-items: start; } }
    h2 { margin: 0; }
    dl { display: grid; grid-template-columns: max-content 1fr; gap: 4px 16px; margin: 0; }
    dt { color: var(--mat-sys-on-surface-variant); }
    dd { margin: 0; }
    .actions { flex-wrap: wrap; }
    .danger { --mat-button-outlined-label-text-color: var(--mat-sys-error); }
    .section { font: var(--mat-sys-title-medium); margin: 24px 0 8px; }
    .entry { display: flex; justify-content: space-between; gap: 12px; padding: 8px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .entry:last-child { border-bottom: none; }
    .small { font-size: 12px; }
  `,
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

  protected onPointsPage(event: PageEvent): void {
    this.pointsPage.set(event.pageIndex + 1);
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
