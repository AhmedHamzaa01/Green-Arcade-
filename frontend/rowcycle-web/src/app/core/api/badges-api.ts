import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_ERROR_TOAST } from '../http-context';
import { Badge, MyBadge, SaveBadgeRequest } from './api.models';

/** The signed-in member's badges (F4). */
@Injectable({ providedIn: 'root' })
export class BadgesApi {
  private readonly http = inject(HttpClient);

  mine(): Observable<MyBadge[]> {
    return this.http.get<MyBadge[]>('/api/v1/me/badges');
  }
}

/** Badge rules (Admin). */
@Injectable({ providedIn: 'root' })
export class AdminBadgesApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Badge[]> {
    return this.http.get<Badge[]>('/api/v1/admin/badges');
  }

  icons(): Observable<string[]> {
    return this.http.get<string[]>('/api/v1/admin/badges/icons');
  }

  // The form shows its own validation errors.
  create(request: SaveBadgeRequest): Observable<Badge> {
    return this.http.post<Badge>('/api/v1/admin/badges', request, formCall());
  }

  update(id: string, request: SaveBadgeRequest): Observable<Badge> {
    return this.http.put<Badge>(`/api/v1/admin/badges/${id}`, request, formCall());
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/admin/badges/${id}`);
  }
}

function formCall() {
  return { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
}
