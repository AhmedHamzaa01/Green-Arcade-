import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
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
} from './api.models';

/** Catalog management (StoreManager, Admin). Forms show their own validation errors. */
@Injectable({ providedIn: 'root' })
export class CatalogAdminApi {
  private readonly http = inject(HttpClient);
  private readonly products = '/api/v1/admin/products';
  private readonly categoriesUrl = '/api/v1/admin/product-categories';
  private readonly formCall = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };

  // Categories
  categories(): Observable<ProductCategory[]> {
    return this.http.get<ProductCategory[]>(this.categoriesUrl);
  }

  createCategory(request: SaveProductCategoryRequest): Observable<ProductCategory> {
    return this.http.post<ProductCategory>(this.categoriesUrl, request, this.formCall);
  }

  updateCategory(id: string, request: SaveProductCategoryRequest): Observable<ProductCategory> {
    return this.http.put<ProductCategory>(`${this.categoriesUrl}/${id}`, request, this.formCall);
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.categoriesUrl}/${id}`);
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
    return this.http.get<PagedResult<AdminProductListItem>>(this.products, { params });
  }

  get(id: string): Observable<AdminProduct> {
    return this.http.get<AdminProduct>(`${this.products}/${id}`);
  }

  create(request: SaveProductRequest): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(this.products, request, this.formCall);
  }

  update(id: string, request: SaveProductRequest): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.products}/${id}`, request, this.formCall);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.products}/${id}`);
  }

  // Variants
  addVariant(productId: string, request: SaveProductVariantRequest): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(`${this.products}/${productId}/variants`, request, this.formCall);
  }

  updateVariant(productId: string, variantId: string, request: SaveProductVariantRequest): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.products}/${productId}/variants/${variantId}`, request, this.formCall);
  }

  deleteVariant(productId: string, variantId: string): Observable<AdminProduct> {
    return this.http.delete<AdminProduct>(`${this.products}/${productId}/variants/${variantId}`);
  }

  // Images
  uploadImage(productId: string, file: File): Observable<AdminProduct> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<AdminProduct>(`${this.products}/${productId}/images`, form);
  }

  deleteImage(productId: string, imageId: string): Observable<AdminProduct> {
    return this.http.delete<AdminProduct>(`${this.products}/${productId}/images/${imageId}`);
  }

  reorderImages(productId: string, imageIds: string[]): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.products}/${productId}/images/order`, { imageIds });
  }
}
