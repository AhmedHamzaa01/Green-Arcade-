import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_TOAST } from '../http-context';
import { PagedResult, ProductCategory, ProductDetail, ProductListItem, ProductQuery } from './models';

// ---- API URLs used by this service ----
const CATEGORIES = `${environment.apiBaseUrl}/product-categories`;
const PRODUCTS = `${environment.apiBaseUrl}/products`;
const product = (slug: string) => `${PRODUCTS}/${encodeURIComponent(slug)}`;

/** The public store: no login needed. */
@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);

  categories(): Observable<ProductCategory[]> {
    return this.http.get<ProductCategory[]>(CATEGORIES);
  }

  products(query: ProductQuery): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams().set('sort', query.sort).set('page', query.page).set('pageSize', query.pageSize);
    if (query.category) {
      params = params.set('category', query.category);
    }
    if (query.search) {
      params = params.set('search', query.search);
    }
    return this.http.get<PagedResult<ProductListItem>>(PRODUCTS, { params });
  }

  /** The detail page shows "not available" itself for a 404, so no error toast. */
  product(slug: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(product(slug), {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }
}
