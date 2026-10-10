import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel, MatPrefix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CatalogAdminApi } from '../../core/api/catalog-admin-api';
import { AdminProductListItem } from '../../core/api/api.models';
import { Notifier } from '../../core/errors/notifier';
import { confirm } from '../../shared/confirm-dialog';
import { EmptyState } from '../../shared/empty-state';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';

/** /admin/products (StoreManager, Admin): every product, including inactive ones. */
@Component({
  selector: 'app-admin-products',
  imports: [
    RouterLink, CurrencyPipe, MatButton, MatIconButton, MatIcon, MatFormField, MatLabel, MatPrefix, MatInput, MatSelect,
    MatOption, MatPaginator, MatTableModule, TranslocoPipe, PageHeader, Loading, EmptyState,
  ],
  template: `
    <div class="page">
      <app-page-header [title]="'admin.catalog.products.title' | transloco" [subtitle]="'admin.catalog.products.subtitle' | transloco">
        <a mat-flat-button class="new" routerLink="/admin/products/new"><mat-icon>add</mat-icon>{{ 'admin.catalog.products.new' | transloco }}</a>
      </app-page-header>

      <div class="filters">
        <mat-form-field subscriptSizing="dynamic" class="search">
          <mat-label>{{ 'common.search' | transloco }}</mat-label>
          <mat-icon matPrefix>search</mat-icon>
          <input #box matInput (keyup.enter)="search.set(box.value); page.set(1)" />
        </mat-form-field>
        <mat-form-field subscriptSizing="dynamic">
          <mat-label>{{ 'admin.catalog.products.category' | transloco }}</mat-label>
          <mat-select [value]="categoryId()" (valueChange)="categoryId.set($event); page.set(1)">
            <mat-option value="">{{ 'common.all' | transloco }}</mat-option>
            @for (c of categories.value() ?? []; track c.id) {
              <mat-option [value]="c.id">{{ c.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </div>

      @if (products.value(); as data) {
        @if (data.total === 0) {
          <app-empty-state icon="inventory_2" [message]="'admin.catalog.products.empty' | transloco" />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="data.items">
              <ng-container matColumnDef="image">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let p">
                  @if (p.imageUrl) { <img class="thumb" [src]="p.imageUrl" alt="" /> } @else { <mat-icon class="muted">image</mat-icon> }
                </td>
              </ng-container>
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.catalog.products.name' | transloco }}</th>
                <td mat-cell *matCellDef="let p">
                  <a [routerLink]="['/admin/products', p.id]">{{ p.name }}</a>
                  <div class="muted small">{{ p.categoryName }}</div>
                </td>
              </ng-container>
              <ng-container matColumnDef="price">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.catalog.products.price' | transloco }}</th>
                <td mat-cell *matCellDef="let p">
                  {{ p.priceEgp | currency: 'EGP' : 'symbol' : '1.0-2' }}
                  @if (p.pricePoints) { <div class="muted small">{{ p.pricePoints }} pts</div> }
                </td>
              </ng-container>
              <ng-container matColumnDef="stock">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.catalog.products.stock' | transloco }}</th>
                <td mat-cell *matCellDef="let p" [class.negative]="p.totalStock === 0">
                  {{ p.totalStock }}
                  <div class="muted small">{{ 'admin.catalog.products.variants' | transloco: { count: p.variantCount } }}</div>
                </td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.catalog.products.status' | transloco }}</th>
                <td mat-cell *matCellDef="let p" [class]="p.isActive ? 'positive' : 'muted'">
                  {{ (p.isActive ? 'common.active' : 'common.inactive') | transloco }}
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let p" class="actions">
                  <a matIconButton [routerLink]="['/admin/products', p.id]" [attr.aria-label]="'common.edit' | transloco"><mat-icon>edit</mat-icon></a>
                  <button matIconButton (click)="remove(p)" [attr.aria-label]="'common.delete' | transloco"><mat-icon>delete</mat-icon></button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
          <mat-paginator [length]="data.total" [pageIndex]="page() - 1" [pageSize]="pageSize()"
            [pageSizeOptions]="[10, 20, 50]" (page)="onPage($event)" />
        }
      } @else if (products.isLoading()) {
        <app-loading />
      }
    </div>
  `,
  styles: `
    .new { margin-top: 12px; }
    .filters { display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 12px; }
    .search { flex: 1 1 240px; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    .thumb { width: 40px; height: 40px; object-fit: cover; border-radius: 6px; display: block; }
    .small { font-size: 12px; }
    .actions { text-align: right; white-space: nowrap; }
  `,
})
export class AdminProducts {
  private readonly api = inject(CatalogAdminApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  protected readonly columns = ['image', 'name', 'price', 'stock', 'status', 'actions'];
  protected readonly search = signal('');
  protected readonly categoryId = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  protected readonly categories = rxResource({ stream: () => this.api.categories() });
  protected readonly products = rxResource({
    params: () => ({ search: this.search(), categoryId: this.categoryId(), page: this.page(), pageSize: this.pageSize() }),
    stream: ({ params }) => this.api.search(params),
  });

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  protected async remove(product: AdminProductListItem): Promise<void> {
    const message = this.transloco.translate('admin.catalog.products.deleteConfirm', { name: product.name });
    if (!(await confirm(this.dialog, { message }))) {
      return;
    }
    try {
      await firstValueFrom(this.api.delete(product.id));
      this.notifier.success('admin.catalog.products.deleted');
      this.products.reload();
    } catch {
      // Shown by the error toast.
    }
  }
}
