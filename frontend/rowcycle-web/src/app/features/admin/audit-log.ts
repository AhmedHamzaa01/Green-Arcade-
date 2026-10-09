import { Component, inject, signal } from '@angular/core';
import { DatePipe, JsonPipe, SlicePipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe } from '@jsverse/transloco';
import { AdminApi } from '../../core/api/admin-api';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

/** GET /admin/audit-logs: who changed what, newest first (FR-19). */
@Component({
  selector: 'app-admin-audit-log',
  imports: [
    DatePipe, JsonPipe, SlicePipe, MatFormField, MatLabel, MatSelect, MatOption, MatPaginator, MatTableModule,
    TranslocoPipe, PageHeader, Loading, EmptyState,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.audit.title' | transloco" [subtitle]="'admin.audit.intro' | transloco" />

      <mat-form-field class="filter">
        <mat-label>{{ 'admin.audit.filter' | transloco }}</mat-label>
        <mat-select [value]="entity()" (valueChange)="onEntity($event)">
          <mat-option value="">{{ 'common.all' | transloco }}</mat-option>
          @for (option of entities; track option) {
            <mat-option [value]="option">{{ option }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      @if (logs.value(); as data) {
        @if (data.total === 0) {
          <app-empty-state icon="history" [message]="'admin.audit.empty' | transloco" />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="data.items">
              <ng-container matColumnDef="when">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.audit.when' | transloco }}</th>
                <td mat-cell *matCellDef="let row">{{ row.createdAt | date: 'short' }}</td>
              </ng-container>
              <ng-container matColumnDef="action">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.audit.action' | transloco }}</th>
                <td mat-cell *matCellDef="let row"><code>{{ row.action }}</code></td>
              </ng-container>
              <ng-container matColumnDef="item">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.audit.item' | transloco }}</th>
                <td mat-cell *matCellDef="let row">{{ row.entity }} / {{ row.entityId }}</td>
              </ng-container>
              <ng-container matColumnDef="user">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.audit.user' | transloco }}</th>
                <td mat-cell *matCellDef="let row" [title]="row.userId">{{ row.userId | slice: 0 : 8 }}</td>
              </ng-container>
              <ng-container matColumnDef="details">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.audit.details' | transloco }}</th>
                <td mat-cell *matCellDef="let row"><code>{{ row.data | json }}</code></td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
          <mat-paginator
            [length]="data.total"
            [pageIndex]="page() - 1"
            [pageSize]="pageSize()"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="onPage($event)" />
        }
      } @else if (logs.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .filter { width: 220px; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    code { font-size: 12px; white-space: nowrap; }
  `,
})
export class AuditLog {
  private readonly api = inject(AdminApi);

  /** Entities that are audit-logged so far; more are added as admin features arrive. */
  protected readonly entities = ['settings'];
  protected readonly columns = ['when', 'action', 'item', 'user', 'details'];

  protected readonly entity = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  protected readonly logs = rxResource({
    params: () => ({ entity: this.entity(), page: this.page(), pageSize: this.pageSize() }),
    stream: ({ params }) => this.api.auditLogs(params),
  });

  protected onEntity(entity: string): void {
    this.entity.set(entity);
    this.page.set(1);
  }

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }
}
