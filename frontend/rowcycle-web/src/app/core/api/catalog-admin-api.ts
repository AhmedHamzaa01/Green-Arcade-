import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_TOAST } from '../http-context';
import {
  AdminProduct,
  AdminProductListItem,
  AdminProductQuery,
  PagedResult,
  ProductCategory,
  SaveProductCategoryRequest,
  SaveProductRequest,
  SaveProductVariantRequest,
} from './models';

// ---- API URLs used by this service ----
const CATEGORIES = `${environment.apiBaseUrl}/admin/product-categories`;
const category = (id: string) => `${CATEGORIES}/${id}`;
const PRODUCTS = `${environment.apiBaseUrl}/admin/products`;
const product = (id: string) => `${PRODUCTS}/${id}`;
const variants = (productId: string) => `${product(productId)}/variants`;
const variant = (productId: string, variantId: string) => `${variants(productId)}/${variantId}`;
const images = (productId: string) => `${product(productId)}/images`;
const image = (productId: string, imageId: string) => `${images(productId)}/${imageId}`;
const imageOrder = (productId: string) => `${images(productId)}/order`;

/** Catalog management (StoreManager, Admin). Forms show their own validation errors. */
@Injectable({ providedIn: 'root' })
export class CatalogAdminApi {
  private readonly http = inject(HttpClient);
  private readonly formCall = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };

  // Categories
  categories(): Observable<ProductCategory[]> {
    return this.http.get<ProductCategory[]>(CATEGORIES);
  }

  createCategory(request: SaveProductCategoryRequest): Observable<ProductCategory> {
    return this.http.post<ProductCategory>(CATEGORIES, request, this.formCall);
  }

  updateCategory(id: string, request: SaveProductCategoryRequest): Observable<ProductCategory> {
    return this.http.put<ProductCategory>(category(id), request, this.formCall);
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(category(id));
  }

  // Products
  search(query: AdminProductQuery): Observable<PagedResult<AdminProductListItem>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }
    return this.http.get<PagedResult<AdminProductListItem>>(PRODUCTS, { params });
  }

  get(id: string): Observable<AdminProduct> {
    return this.http.get<AdminProduct>(product(id));
  }

  create(request: SaveProductRequest): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(PRODUCTS, request, this.formCall);
  }

  update(id: string, request: SaveProductRequest): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(product(id), request, this.formCall);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(product(id));
  }

  // Variants
  addVariant(productId: string, request: SaveProductVariantRequest): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(variants(productId), request, this.formCall);
  }

  updateVariant(productId: string, variantId: string, request: SaveProductVariantRequest): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(variant(productId, variantId), request, this.formCall);
  }

  deleteVariant(productId: string, variantId: string): Observable<AdminProduct> {
    return this.http.delete<AdminProduct>(variant(productId, variantId));
  }

  // Images
  uploadImage(productId: string, file: File): Observable<AdminProduct> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<AdminProduct>(images(productId), form);
  }

  deleteImage(productId: string, imageId: string): Observable<AdminProduct> {
    return this.http.delete<AdminProduct>(image(productId, imageId));
  }

  reorderImages(productId: string, imageIds: string[]): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(imageOrder(productId), { imageIds });
  }
}
