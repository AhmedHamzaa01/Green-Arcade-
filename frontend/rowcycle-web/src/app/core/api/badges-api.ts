import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_TOAST } from '../http-context';
import { Badge, MyBadge, SaveBadgeRequest } from './models';

// ---- API URLs used by these services ----
const MY_BADGES = `${environment.apiBaseUrl}/me/badges`;
const BADGES = `${environment.apiBaseUrl}/admin/badges`;
const BADGE_ICONS = `${BADGES}/icons`;
const badge = (id: string) => `${BADGES}/${id}`;

/** The signed-in member's badges (F4). */
@Injectable({ providedIn: 'root' })
export class BadgesApi {
  private readonly http = inject(HttpClient);

  mine(): Observable<MyBadge[]> {
    return this.http.get<MyBadge[]>(MY_BADGES);
  }
}

/** Badge rules (Admin). */
@Injectable({ providedIn: 'root' })
export class AdminBadgesApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Badge[]> {
    return this.http.get<Badge[]>(BADGES);
  }

  icons(): Observable<string[]> {
    return this.http.get<string[]>(BADGE_ICONS);
  }

  // The form shows its own validation errors.
  create(request: SaveBadgeRequest): Observable<Badge> {
    return this.http.post<Badge>(BADGES, request, formCall());
  }

  update(id: string, request: SaveBadgeRequest): Observable<Badge> {
    return this.http.put<Badge>(badge(id), request, formCall());
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(badge(id));
  }
}

function formCall() {
  return { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
}
