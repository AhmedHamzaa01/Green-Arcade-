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
import { CatalogAdminApi } from '../../../../core/api/catalog-admin-api';
import { AdminProduct, AdminProductVariant, ProductCategory } from '../../../../core/api/models';
import { Notifier } from '../../../../core/errors/notifier';
import { applyServerErrors, toProblem } from '../../../../core/errors/problem';
import { confirm } from '../../../../shared/ui/confirm-dialog/confirm-dialog';
import { FieldErrorPipe } from '../../../../shared/pipes/field-error.pipe';
import { Loading } from '../../../../shared/ui/loading/loading';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { VariantDialog } from '../../components/variant-dialog/variant-dialog';

/** /admin/products/new and /admin/products/:id — details, variants (stock) and photos. */
@Component({
  selector: 'app-product-edit',
  imports: [
    ReactiveFormsModule, RouterLink, CurrencyPipe, MatButton, MatIconButton, MatCard, MatCardContent, MatFormField,
    MatLabel, MatHint, MatError, MatIcon, MatInput, MatSelect, MatOption, MatSlideToggle, MatTableModule, TranslocoPipe,
    FieldErrorPipe, PageHeader, Loading,
  ],
  templateUrl: './product-edit.html',
  styleUrl: './product-edit.scss',
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
