import { Component, OnInit, inject, input, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CatalogAdminApi } from '../../core/api/catalog-admin-api';
import { AdminProduct, AdminProductVariant, ProductCategory } from '../../core/api/api.models';
import { Notifier } from '../../core/errors/notifier';
import { applyServerErrors, toProblem } from '../../core/errors/problem';
import { confirm } from '../../shared/confirm-dialog';
import { FieldErrorPipe } from '../../shared/field-error.pipe';
import { Loading } from '../../shared/loading';
import { PageHeader } from '../../shared/page-header';
import { VariantDialog } from './variant-dialog';

/** /admin/products/new and /admin/products/:id — details, variants (stock) and photos. */
@Component({
  selector: 'app-product-edit',
  imports: [
    ReactiveFormsModule, RouterLink, CurrencyPipe, MatButton, MatIconButton, MatCard, MatCardContent, MatFormField,
    MatLabel, MatHint, MatError, MatIcon, MatInput, MatSelect, MatOption, MatSlideToggle, MatTableModule, TranslocoPipe,
    FieldErrorPipe, PageHeader, Loading,
  ],
  template: `
    <div class="page">
      <a mat-button routerLink="/admin/products"><mat-icon>arrow_back</mat-icon>{{ 'nav.products' | transloco }}</a>
      <app-page-header [title]="(id() ? 'admin.catalog.edit.editTitle' : 'admin.catalog.edit.newTitle') | transloco">
        @if (product(); as p) {
          @if (p.isActive) {
            <a mat-button [routerLink]="['/products', p.slug]" target="_blank"><mat-icon>open_in_new</mat-icon>{{ 'admin.catalog.edit.viewInStore' | transloco }}</a>
          }
        }
      </app-page-header>

      @if (ready()) {
        <!-- Details -->
        <mat-card>
          <mat-card-content>
            <h2>{{ 'admin.catalog.edit.details' | transloco }}</h2>
            <form class="form-grid" [formGroup]="form" (ngSubmit)="save()">
              <mat-form-field class="wide">
                <mat-label>{{ 'admin.catalog.edit.name' | transloco }}</mat-label>
                <input matInput formControlName="name" />
                <mat-error>{{ form.controls.name | fieldError }}</mat-error>
              </mat-form-field>
              <mat-form-field>
                <mat-label>{{ 'admin.catalog.edit.category' | transloco }}</mat-label>
                <mat-select formControlName="categoryId">
                  @for (c of categories(); track c.id) {
                    <mat-option [value]="c.id">{{ c.name }}</mat-option>
                  }
                </mat-select>
                <mat-error>{{ form.controls.categoryId | fieldError }}</mat-error>
              </mat-form-field>
              <mat-form-field>
                <mat-label>{{ 'admin.catalog.edit.slug' | transloco }}</mat-label>
                <input matInput formControlName="slug" />
                <mat-hint>{{ 'admin.catalog.edit.slugHint' | transloco }}</mat-hint>
                <mat-error>{{ form.controls.slug | fieldError }}</mat-error>
              </mat-form-field>
              <mat-form-field class="wide">
                <mat-label>{{ 'admin.catalog.edit.description' | transloco }}</mat-label>
                <textarea matInput rows="4" formControlName="description"></textarea>
                <mat-error>{{ form.controls.description | fieldError }}</mat-error>
              </mat-form-field>
              <mat-form-field>
                <mat-label>{{ 'admin.catalog.edit.priceEgp' | transloco }}</mat-label>
                <input matInput type="number" min="0" step="0.01" formControlName="priceEgp" />
                <mat-error>{{ form.controls.priceEgp | fieldError }}</mat-error>
              </mat-form-field>
              <mat-form-field>
                <mat-label>{{ 'admin.catalog.edit.pricePoints' | transloco }}</mat-label>
                <input matInput type="number" min="1" formControlName="pricePoints" />
                <mat-hint>{{ 'admin.catalog.edit.pricePointsHint' | transloco }}</mat-hint>
                <mat-error>{{ form.controls.pricePoints | fieldError }}</mat-error>
              </mat-form-field>
              <mat-form-field>
                <mat-label>{{ 'admin.catalog.edit.rewardPoints' | transloco }}</mat-label>
                <input matInput type="number" min="0" formControlName="rewardPoints" />
                <mat-hint>{{ 'admin.catalog.edit.rewardPointsHint' | transloco }}</mat-hint>
                <mat-error>{{ form.controls.rewardPoints | fieldError }}</mat-error>
              </mat-form-field>
              <mat-slide-toggle class="wide" formControlName="isActive">{{ 'admin.catalog.edit.active' | transloco }}</mat-slide-toggle>
              @if (error()) {
                <p class="form-error wide" role="alert">{{ error() }}</p>
              }
              <div class="wide">
                <button mat-flat-button type="submit" [disabled]="saving()">{{ (id() ? 'common.save' : 'common.create') | transloco }}</button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>

        @if (product(); as p) {
          <!-- Variants and stock -->
          <mat-card>
            <mat-card-content>
              <div class="section-head">
                <div>
                  <h2>{{ 'admin.catalog.edit.variants' | transloco }}</h2>
                  <p class="muted">{{ 'admin.catalog.edit.variantsHint' | transloco }}</p>
                </div>
                <button mat-stroked-button (click)="editVariant(null)"><mat-icon>add</mat-icon>{{ 'admin.catalog.edit.addVariant' | transloco }}</button>
              </div>
              <div class="table-wrap">
                <table mat-table [dataSource]="p.variants">
                  <ng-container matColumnDef="name">
                    <th mat-header-cell *matHeaderCellDef>{{ 'admin.catalog.edit.variantName' | transloco }}</th>
                    <td mat-cell *matCellDef="let v">{{ v.name }}<div class="muted small">{{ v.sku }}</div></td>
                  </ng-container>
                  <ng-container matColumnDef="stock">
                    <th mat-header-cell *matHeaderCellDef>{{ 'admin.catalog.edit.stock' | transloco }}</th>
                    <td mat-cell *matCellDef="let v" [class.negative]="v.stock === 0">{{ v.stock }}</td>
                  </ng-container>
                  <ng-container matColumnDef="price">
                    <th mat-header-cell *matHeaderCellDef>{{ 'admin.catalog.products.price' | transloco }}</th>
                    <td mat-cell *matCellDef="let v">{{ (v.priceOverride ?? p.priceEgp) | currency: 'EGP' : 'symbol' : '1.0-2' }}</td>
                  </ng-container>
                  <ng-container matColumnDef="actions">
                    <th mat-header-cell *matHeaderCellDef></th>
                    <td mat-cell *matCellDef="let v" class="actions">
                      <button matIconButton (click)="editVariant(v)" [attr.aria-label]="'common.edit' | transloco"><mat-icon>edit</mat-icon></button>
                      <button matIconButton (click)="deleteVariant(v)" [disabled]="p.variants.length === 1" [attr.aria-label]="'common.delete' | transloco"><mat-icon>delete</mat-icon></button>
                    </td>
                  </ng-container>
                  <tr mat-header-row *matHeaderRowDef="variantColumns"></tr>
                  <tr mat-row *matRowDef="let row; columns: variantColumns"></tr>
                </table>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Photos -->
          <mat-card>
            <mat-card-content>
              <div class="section-head">
                <div>
                  <h2>{{ 'admin.catalog.edit.images' | transloco }}</h2>
                  <p class="muted">{{ 'admin.catalog.edit.imagesHint' | transloco }}</p>
                </div>
                <button mat-stroked-button (click)="fileInput.click()" [disabled]="uploading()">
                  <mat-icon>upload</mat-icon>{{ 'admin.catalog.edit.upload' | transloco }}
                </button>
                <input #fileInput hidden type="file" accept="image/jpeg,image/png,image/webp" (change)="upload(fileInput)" />
              </div>
              @if (p.images.length === 0) {
                <p class="muted">{{ 'admin.catalog.edit.noImages' | transloco }}</p>
              } @else {
                <div class="images">
                  @for (image of p.images; track image.id; let i = $index; let last = $last) {
                    <div class="image" [class.main]="i === 0">
                      <img [src]="image.url" alt="" />
                      <div class="image-actions">
                        <button matIconButton [disabled]="i === 0" (click)="move(i, -1)" [attr.aria-label]="'admin.catalog.edit.moveLeft' | transloco"><mat-icon>chevron_left</mat-icon></button>
                        <button matIconButton (click)="deleteImage(image.id)" [attr.aria-label]="'common.delete' | transloco"><mat-icon>delete</mat-icon></button>
                        <button matIconButton [disabled]="last" (click)="move(i, 1)" [attr.aria-label]="'admin.catalog.edit.moveRight' | transloco"><mat-icon>chevron_right</mat-icon></button>
                      </div>
                    </div>
                  }
                </div>
              }
            </mat-card-content>
          </mat-card>
        } @else {
          <p class="muted">{{ 'admin.catalog.edit.saveFirst' | transloco }}</p>
        }
      } @else {
        <app-loading />
      }
    </div>
  `,
  styles: `
    mat-card { margin-bottom: 16px; }
    h2 { font: var(--mat-sys-title-medium); margin: 0 0 8px; }
    .form-grid { display: grid; gap: 4px 16px; grid-template-columns: 1fr; }
    @media (min-width: 720px) { .form-grid { grid-template-columns: 1fr 1fr; } .wide { grid-column: 1 / -1; } }
    .section-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; flex-wrap: wrap; margin-bottom: 8px; }
    .section-head p { margin: 0; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    .small { font-size: 12px; }
    .actions { text-align: right; white-space: nowrap; }
    .images { display: grid; gap: 12px; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); }
    .image { border: 2px solid var(--mat-sys-outline-variant); border-radius: 12px; overflow: hidden; }
    .image.main { border-color: var(--mat-sys-primary); }
    .image img { width: 100%; aspect-ratio: 1; object-fit: cover; display: block; }
    .image-actions { display: flex; justify-content: space-between; }
  `,
})
export class ProductEdit implements OnInit {
  private readonly api = inject(CatalogAdminApi);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly transloco = inject(TranslocoService);

  /** From the URL; empty on /admin/products/new. */
  readonly id = input<string>();

  protected readonly variantColumns = ['name', 'stock', 'price', 'actions'];
  protected readonly categories = signal<ProductCategory[]>([]);
  protected readonly product = signal<AdminProduct | null>(null);
  protected readonly ready = signal(false);
  protected readonly saving = signal(false);
  protected readonly uploading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).group({
    categoryId: ['', Validators.required],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    slug: [''],
    description: ['', Validators.maxLength(4000)],
    priceEgp: [0, [Validators.required, Validators.min(0)]],
    pricePoints: [null as number | null, Validators.min(1)],
    rewardPoints: [0, [Validators.required, Validators.min(0)]],
    isActive: [true],
  });

  async ngOnInit(): Promise<void> {
    this.categories.set(await firstValueFrom(this.api.categories()));
    const id = this.id();
    if (id) {
      this.show(await firstValueFrom(this.api.get(id)));
    }
    this.ready.set(true);
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    const request = {
      categoryId: v.categoryId ?? '',
      name: v.name ?? '',
      slug: v.slug?.trim() || null,
      description: v.description ?? '',
      priceEgp: Number(v.priceEgp ?? 0),
      pricePoints: v.pricePoints ? Number(v.pricePoints) : null,
      rewardPoints: Number(v.rewardPoints ?? 0),
      isActive: v.isActive ?? true,
    };
    try {
      const id = this.id();
      if (id) {
        this.show(await firstValueFrom(this.api.update(id, request)));
        this.notifier.success('admin.catalog.edit.saved');
      } else {
        const created = await firstValueFrom(this.api.create(request));
        this.notifier.success('admin.catalog.edit.created');
        await this.router.navigate(['/admin/products', created.id], { replaceUrl: true });
      }
    } catch (e) {
      const problem = toProblem(e);
      if (!applyServerErrors(this.form, problem)) {
        this.error.set(problem.detail ?? problem.title ?? this.transloco.translate('errors.generic'));
      }
    } finally {
      this.saving.set(false);
    }
  }

  protected async editVariant(variant: AdminProductVariant | null): Promise<void> {
    const product = this.product()!;
    const updated = await firstValueFrom(
      this.dialog.open(VariantDialog, { data: { productId: product.id, variant }, width: '420px' }).afterClosed());
    if (updated) {
      this.product.set(updated);
      this.notifier.success('admin.catalog.edit.variantSaved');
    }
  }

  protected async deleteVariant(variant: AdminProductVariant): Promise<void> {
    const message = this.transloco.translate('admin.catalog.edit.deleteVariantConfirm', { name: variant.name });
    if (await confirm(this.dialog, { message })) {
      await this.run(() => this.api.deleteVariant(this.product()!.id, variant.id), 'admin.catalog.edit.variantDeleted');
    }
  }

  protected async upload(input: HTMLInputElement): Promise<void> {
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }
    this.uploading.set(true);
    // Type and size are checked again by the API, which reads the file's content.
    await this.run(() => this.api.uploadImage(this.product()!.id, file), 'admin.catalog.edit.uploaded');
    this.uploading.set(false);
  }

  protected async deleteImage(imageId: string): Promise<void> {
    await this.run(() => this.api.deleteImage(this.product()!.id, imageId), 'admin.catalog.edit.imageDeleted');
  }

  /** Swaps the photo at <index> with its neighbour; the first photo is the main one. */
  protected async move(index: number, step: -1 | 1): Promise<void> {
    const ids = this.product()!.images.map((i) => i.id);
    [ids[index], ids[index + step]] = [ids[index + step], ids[index]];
    await this.run(() => this.api.reorderImages(this.product()!.id, ids));
  }

  private show(product: AdminProduct): void {
    this.product.set(product);
    this.form.reset({
      categoryId: product.categoryId,
      name: product.name,
      slug: product.slug,
      description: product.description,
      priceEgp: product.priceEgp,
      pricePoints: product.pricePoints,
      rewardPoints: product.rewardPoints,
      isActive: product.isActive,
    });
  }

  /** Calls the API, shows the updated product, and an optional success message. Errors are shown by the toast. */
  private async run(call: () => ReturnType<CatalogAdminApi['get']>, successKey?: string): Promise<void> {
    try {
      this.product.set(await firstValueFrom(call()));
      if (successKey) {
        this.notifier.success(successKey);
      }
    } catch {
      // Already shown by the error toast (e.g. "Unsupported image. Use a JPG, PNG or WEBP image.").
    }
  }
}
