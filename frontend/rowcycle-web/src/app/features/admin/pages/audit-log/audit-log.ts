import { Component, inject, signal } from '@angular/core';
import { DatePipe, JsonPipe, SlicePipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe } from '@jsverse/transloco';
import { AdminApi } from '../../../../core/api/admin-api';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';

/** GET /admin/audit-logs: who changed what, newest first (FR-19). */
@Component({
  selector: 'app-admin-audit-log',
  imports: [
    DatePipe, JsonPipe, SlicePipe, MatFormField, MatLabel, MatSelect, MatOption, MatPaginator, MatTableModule,
    TranslocoPipe, PageHeader, Loading, EmptyState,
  ],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.scss',
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
