import { Component, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe } from '@jsverse/transloco';
import { DashboardApi } from '../../core/api/users-admin-api';
import { AuthSession } from '../../core/auth/auth-session';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

/** /admin/dashboard — the staff home screen (F8). Each card links to the page where the work is done. */
@Component({
  selector: 'app-admin-dashboard',
  imports: [DecimalPipe, RouterLink, MatCard, MatCardContent, MatIcon, MatTableModule, TranslocoPipe, PageHeader, Loading],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.dashboard.title' | transloco" />
      @if (dashboard.value(); as d) {
        <div class="cards">
          <a class="card-link" [routerLink]="canModerate() ? '/admin/submissions' : null">
            <mat-card>
              <mat-card-content>
                <mat-icon>fact_check</mat-icon>
                <div class="count">{{ d.pendingSubmissions | number }}</div>
                <div class="muted">{{ 'admin.dashboard.pendingSubmissions' | transloco }}</div>
              </mat-card-content>
            </mat-card>
          </a>
          <a class="card-link" [routerLink]="canManageStore() ? '/admin/orders' : null">
            <mat-card>
              <mat-card-content>
                <mat-icon>local_shipping</mat-icon>
                <div class="count">{{ d.newOrders | number }}</div>
                <div class="muted">{{ 'admin.dashboard.newOrders' | transloco }}</div>
              </mat-card-content>
            </mat-card>
          </a>
          <a class="card-link" [routerLink]="canManageStore() ? '/admin/products' : null">
            <mat-card>
              <mat-card-content>
                <mat-icon [class.negative]="d.lowStockCount > 0">inventory_2</mat-icon>
                <div class="count">{{ d.lowStockCount | number }}</div>
                <div class="muted">{{ 'admin.dashboard.lowStock' | transloco: { threshold: d.lowStockThreshold } }}</div>
              </mat-card-content>
            </mat-card>
          </a>
        </div>

        @if (d.lowStock.length > 0) {
          <h2 class="section">{{ 'admin.dashboard.lowStockTitle' | transloco }}</h2>
          <div class="table-wrap">
            <table mat-table [dataSource]="d.lowStock">
              <ng-container matColumnDef="product">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.dashboard.product' | transloco }}</th>
                <td mat-cell *matCellDef="let i">
                  @if (canManageStore()) { <a [routerLink]="['/admin/products', i.productId]">{{ i.productName }}</a> }
                  @else { {{ i.productName }} }
                  <div class="muted small">{{ i.variantName }} · {{ i.sku }}</div>
                </td>
              </ng-container>
              <ng-container matColumnDef="stock">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.dashboard.stock' | transloco }}</th>
                <td mat-cell *matCellDef="let i" [class.negative]="i.stock === 0">{{ i.stock }}</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
          @if (d.lowStockCount > d.lowStock.length) {
            <p class="muted small">{{ 'admin.dashboard.more' | transloco: { count: d.lowStockCount - d.lowStock.length } }}</p>
          }
        }
      } @else if (dashboard.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .cards { display: grid; gap: 12px; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); }
    .card-link { text-decoration: none; color: inherit; }
    .card-link:not([href]) { cursor: default; }
    mat-card-content mat-icon { color: var(--mat-sys-primary); }
    mat-card-content mat-icon.negative { color: var(--mat-sys-error); }
    .count { font: var(--mat-sys-display-small); margin: 4px 0; }
    .section { font: var(--mat-sys-title-medium); margin: 24px 0 8px; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    .small { font-size: 12px; }
  `,
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
