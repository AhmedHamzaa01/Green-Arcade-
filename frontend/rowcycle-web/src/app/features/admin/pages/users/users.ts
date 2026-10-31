import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatChip, MatChipSet } from '@angular/material/chips';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe } from '@jsverse/transloco';
import { Role } from '../../../../core/api/models';
import { UsersAdminApi } from '../../../../core/api/users-admin-api';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { FilterBar } from '../../../../shared/ui/filter-bar/filter-bar';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

export const ALL_ROLES: Role[] = ['Member', 'Moderator', 'StoreManager', 'Admin'];

/** /admin/users (Admin): find people, then open one to change roles, block, or adjust points. */
@Component({
  selector: 'app-admin-users',
  imports: [
    DecimalPipe, RouterLink, MatChip, MatChipSet, MatFormField, MatLabel, MatSelect, MatOption,
    MatPaginator, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState, FilterBar,
  ],
  templateUrl: './users.html',
  styleUrl: './users.scss',
})
export class AdminUsers {
  private readonly api = inject(UsersAdminApi);

  protected readonly roles = ALL_ROLES;
  protected readonly columns = ['name', 'roles', 'status', 'points'];
  protected readonly search = signal('');
  protected readonly role = signal<Role | ''>('');
  protected readonly blocked = signal<boolean | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  protected readonly users = rxResource({
    params: () => ({ search: this.search(), role: this.role(), blocked: this.blocked(), page: this.page(), pageSize: this.pageSize() }),
    stream: ({ params }) => this.api.search(params),
  });

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  // Every filter change starts again from the first page.
  protected onSearch(text: string | null): void {
    this.search.set(text ?? '');
    this.page.set(1);
  }

  protected onRoleChange(role: Role | ''): void {
    this.role.set(role);
    this.page.set(1);
  }

  protected onBlockedChange(blocked: boolean | null): void {
    this.blocked.set(blocked);
    this.page.set(1);
  }
}
