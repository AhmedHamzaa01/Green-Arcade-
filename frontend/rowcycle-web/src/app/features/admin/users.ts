import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatChip, MatChipSet } from '@angular/material/chips';
import { MatFormField, MatLabel, MatPrefix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe } from '@jsverse/transloco';
import { Role } from '../../core/api/api.models';
import { UsersAdminApi } from '../../core/api/users-admin-api';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

export const ALL_ROLES: Role[] = ['Member', 'Moderator', 'StoreManager', 'Admin'];

/** /admin/users (Admin): find people, then open one to change roles, block, or adjust points. */
@Component({
  selector: 'app-admin-users',
  imports: [
    DecimalPipe, RouterLink, MatChip, MatChipSet, MatFormField, MatLabel, MatPrefix, MatIcon, MatInput, MatSelect, MatOption,
    MatPaginator, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.users.title' | transloco" [subtitle]="'admin.users.subtitle' | transloco" />

      <div class="filters">
        <mat-form-field class="search" subscriptSizing="dynamic">
          <mat-label>{{ 'admin.users.search' | transloco }}</mat-label>
          <mat-icon matPrefix>search</mat-icon>
          <input #box matInput (keyup.enter)="search.set(box.value.trim()); page.set(1)" />
        </mat-form-field>
        <mat-form-field subscriptSizing="dynamic">
          <mat-label>{{ 'admin.users.role' | transloco }}</mat-label>
          <mat-select [value]="role()" (valueChange)="role.set($event); page.set(1)">
            <mat-option value="">{{ 'common.all' | transloco }}</mat-option>
            @for (r of roles; track r) {
              <mat-option [value]="r">{{ 'roles.' + r | transloco }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field subscriptSizing="dynamic">
          <mat-label>{{ 'admin.users.status' | transloco }}</mat-label>
          <mat-select [value]="blocked()" (valueChange)="blocked.set($event); page.set(1)">
            <mat-option [value]="null">{{ 'common.all' | transloco }}</mat-option>
            <mat-option [value]="false">{{ 'admin.users.active' | transloco }}</mat-option>
            <mat-option [value]="true">{{ 'admin.users.blocked' | transloco }}</mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      @if (users.value(); as data) {
        @if (data.total === 0) {
          <app-empty-state icon="group" [message]="'admin.users.empty' | transloco" />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="data.items">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.users.name' | transloco }}</th>
                <td mat-cell *matCellDef="let u">
                  <a [routerLink]="['/admin/users', u.id]">{{ u.fullName }}</a>
                  <div class="muted small">{{ u.email }}</div>
                </td>
              </ng-container>
              <ng-container matColumnDef="roles">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.users.roles' | transloco }}</th>
                <td mat-cell *matCellDef="let u">
                  <mat-chip-set>
                    @for (r of u.roles; track r) { <mat-chip>{{ 'roles.' + r | transloco }}</mat-chip> }
                  </mat-chip-set>
                </td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.users.status' | transloco }}</th>
                <td mat-cell *matCellDef="let u" [class]="u.isActive ? 'positive' : 'negative'">
                  {{ (u.isActive ? 'admin.users.active' : 'admin.users.blocked') | transloco }}
                  @if (!u.emailVerified) { <div class="muted small">{{ 'admin.users.unverified' | transloco }}</div> }
                </td>
              </ng-container>
              <ng-container matColumnDef="points">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.users.points' | transloco }}</th>
                <td mat-cell *matCellDef="let u">{{ u.pointsBalance | number }}</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
          <mat-paginator [length]="data.total" [pageIndex]="page() - 1" [pageSize]="pageSize()" [pageSizeOptions]="[20, 50, 100]" (page)="onPage($event)" />
        }
      } @else if (users.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .filters { display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 12px; }
    .search { flex: 1 1 260px; max-width: 400px; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    .small { font-size: 12px; }
  `,
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
}
