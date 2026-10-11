import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_ERROR_TOAST } from '../http-context';
import { Address, Cart, Order, OrderStatus, OrderSummary, PagedResult, PaymentMethod, SaveAddressRequest } from './api.models';

/** Addresses, cart and the member's orders. */
@Injectable({ providedIn: 'root' })
export class ShopApi {
  private readonly http = inject(HttpClient);

  // Addresses — the form shows its own validation errors.
  addresses(): Observable<Address[]> {
    return this.http.get<Address[]>('/api/v1/me/addresses');
  }

  createAddress(request: SaveAddressRequest): Observable<Address> {
    return this.http.post<Address>('/api/v1/me/addresses', request, formCall());
  }

  updateAddress(id: string, request: SaveAddressRequest): Observable<Address> {
    return this.http.put<Address>(`/api/v1/me/addresses/${id}`, request, formCall());
  }

  deleteAddress(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/me/addresses/${id}`);
  }

  // Cart
  cart(): Observable<Cart> {
    return this.http.get<Cart>('/api/v1/cart');
  }

  addToCart(variantId: string, quantity: number): Observable<Cart> {
    return this.http.post<Cart>('/api/v1/cart/items', { variantId, quantity });
  }

  updateCartItem(variantId: string, quantity: number): Observable<Cart> {
    return this.http.put<Cart>(`/api/v1/cart/items/${variantId}`, { quantity });
  }

  removeCartItem(variantId: string): Observable<Cart> {
    return this.http.delete<Cart>(`/api/v1/cart/items/${variantId}`);
  }

  // Orders
  checkout(addressId: string, paymentMethod: PaymentMethod): Observable<Order> {
    return this.http.post<Order>('/api/v1/orders', { addressId, paymentMethod });
  }

  myOrders(page: number, pageSize: number): Observable<PagedResult<OrderSummary>> {
    return this.http.get<PagedResult<OrderSummary>>('/api/v1/me/orders', { params: { page, pageSize } });
  }

  myOrder(id: string): Observable<Order> {
    return this.http.get<Order>(`/api/v1/me/orders/${id}`);
  }

  cancelMyOrder(id: string): Observable<Order> {
    return this.http.post<Order>(`/api/v1/me/orders/${id}/cancel`, null);
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
    return this.http.get<PagedResult<OrderSummary>>('/api/v1/admin/orders', { params });
  }

  get(id: string): Observable<Order> {
    return this.http.get<Order>(`/api/v1/admin/orders/${id}`);
  }

  changeStatus(id: string, status: OrderStatus): Observable<Order> {
    return this.http.put<Order>(`/api/v1/admin/orders/${id}/status`, { status });
  }
}

function formCall() {
  return { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
}
