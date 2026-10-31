import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_TOAST } from '../http-context';
import { Address, Cart, Order, OrderStatus, OrderSummary, PagedResult, PaymentMethod, SaveAddressRequest } from './models';

// ---- API URLs used by these services ----
const MY_ADDRESSES = `${environment.apiBaseUrl}/me/addresses`;
const myAddress = (id: string) => `${MY_ADDRESSES}/${id}`;
const CART = `${environment.apiBaseUrl}/cart`;
const CART_ITEMS = `${CART}/items`;
const cartItem = (variantId: string) => `${CART_ITEMS}/${variantId}`;
const ORDERS = `${environment.apiBaseUrl}/orders`;
const MY_ORDERS = `${environment.apiBaseUrl}/me/orders`;
const myOrder = (id: string) => `${MY_ORDERS}/${id}`;
const cancelMyOrder = (id: string) => `${myOrder(id)}/cancel`;
const ADMIN_ORDERS = `${environment.apiBaseUrl}/admin/orders`;
const adminOrder = (id: string) => `${ADMIN_ORDERS}/${id}`;
const adminOrderStatus = (id: string) => `${adminOrder(id)}/status`;

/** Addresses, cart and the member's orders. */
@Injectable({ providedIn: 'root' })
export class ShopApi {
  private readonly http = inject(HttpClient);

  // Addresses — the form shows its own validation errors.
  addresses(): Observable<Address[]> {
    return this.http.get<Address[]>(MY_ADDRESSES);
  }

  createAddress(request: SaveAddressRequest): Observable<Address> {
    return this.http.post<Address>(MY_ADDRESSES, request, formCall());
  }

  updateAddress(id: string, request: SaveAddressRequest): Observable<Address> {
    return this.http.put<Address>(myAddress(id), request, formCall());
  }

  deleteAddress(id: string): Observable<void> {
    return this.http.delete<void>(myAddress(id));
  }

  // Cart
  cart(): Observable<Cart> {
    return this.http.get<Cart>(CART);
  }

  addToCart(variantId: string, quantity: number): Observable<Cart> {
    return this.http.post<Cart>(CART_ITEMS, { variantId, quantity });
  }

  updateCartItem(variantId: string, quantity: number): Observable<Cart> {
    return this.http.put<Cart>(cartItem(variantId), { quantity });
  }

  removeCartItem(variantId: string): Observable<Cart> {
    return this.http.delete<Cart>(cartItem(variantId));
  }

  // Orders
  checkout(addressId: string, paymentMethod: PaymentMethod): Observable<Order> {
    return this.http.post<Order>(ORDERS, { addressId, paymentMethod });
  }

  myOrders(page: number, pageSize: number): Observable<PagedResult<OrderSummary>> {
    return this.http.get<PagedResult<OrderSummary>>(MY_ORDERS, { params: { page, pageSize } });
  }

  myOrder(id: string): Observable<Order> {
    return this.http.get<Order>(myOrder(id));
  }

  cancelMyOrder(id: string): Observable<Order> {
    return this.http.post<Order>(cancelMyOrder(id), null);
  }
}

/** Staff order handling (StoreManager, Admin). */
@Injectable({ providedIn: 'root' })
export class AdminOrdersApi {
  private readonly http = inject(HttpClient);

  search(query: { status?: OrderStatus | ''; search?: string; page: number; pageSize: number }): Observable<PagedResult<OrderSummary>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.search) {
      params = params.set('search', query.search);
    }
    return this.http.get<PagedResult<OrderSummary>>(ADMIN_ORDERS, { params });
  }

  get(id: string): Observable<Order> {
    return this.http.get<Order>(adminOrder(id));
  }

  changeStatus(id: string, status: OrderStatus): Observable<Order> {
    return this.http.put<Order>(adminOrderStatus(id), { status });
  }
}

function formCall() {
  return { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
}
