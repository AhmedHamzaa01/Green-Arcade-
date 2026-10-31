import { Component, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe } from '@jsverse/transloco';
import { DashboardApi } from '../../../../core/api/users-admin-api';
import { AuthSession } from '../../../../core/auth/auth-session';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { StatCard } from '../../../../shared/ui/stat-card/stat-card';

/** /admin/dashboard — the staff home screen (F8). Each card links to the page where the work is done. */
@Component({
  selector: 'app-admin-dashboard',
  imports: [DecimalPipe, RouterLink, MatTableModule, TranslocoPipe, PageHeader, Loading, StatCard],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class AdminDashboard {
  private readonly api = inject(DashboardApi);
  private readonly session = inject(AuthSession);

  protected readonly columns = ['product', 'stock'];
  protected readonly dashboard = rxResource({ stream: () => this.api.get() });

  // Links only to pages the person can open.
  protected canModerate(): boolean {
    return this.session.hasRole('Moderator', 'Admin');
  }

  protected canManageStore(): boolean {
    return this.session.hasRole('StoreManager', 'Admin');
  }
}
