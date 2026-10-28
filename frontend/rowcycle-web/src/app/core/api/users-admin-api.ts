import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_ERROR_TOAST } from '../http-context';
import { AdminUserDetail, AdminUserListItem, Dashboard, PagedResult, PointsChange, PointsHistoryResponse, Role, UserQuery } from './api.models';

/** Users for admins (F8): search, roles, block, manual points. */
@Injectable({ providedIn: 'root' })
export class UsersAdminApi {
  private readonly http = inject(HttpClient);

  search(query: UserQuery): Observable<PagedResult<AdminUserListItem>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.role) {
      params = params.set('role', query.role);
    }
    if (query.blocked !== null && query.blocked !== undefined) {
      params = params.set('blocked', query.blocked);
    }
    return this.http.get<PagedResult<AdminUserListItem>>('/api/v1/admin/users', { params });
  }

  get(id: string): Observable<AdminUserDetail> {
    return this.http.get<AdminUserDetail>(`/api/v1/admin/users/${id}`);
  }

  points(id: string, page: number, pageSize: number): Observable<PointsHistoryResponse> {
    return this.http.get<PointsHistoryResponse>(`/api/v1/admin/users/${id}/points`, { params: { page, pageSize } });
  }

  setRoles(id: string, roles: Role[]): Observable<AdminUserDetail> {
    return this.http.put<AdminUserDetail>(`/api/v1/admin/users/${id}/roles`, { roles });
  }

  setBlocked(id: string, blocked: boolean): Observable<AdminUserDetail> {
    return this.http.put<AdminUserDetail>(`/api/v1/admin/users/${id}/block`, { blocked });
  }

  /** The dialog shows the API's errors itself (e.g. "Not enough points."). */
  adjustPoints(id: string, amount: number, reason: string): Observable<PointsChange> {
    return this.http.post<PointsChange>(`/api/v1/admin/users/${id}/points-adjustments`, { amount, reason }, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }
}

/** The staff home screen (Moderator, StoreManager, Admin). */
@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);

  get(): Observable<Dashboard> {
    return this.http.get<Dashboard>('/api/v1/admin/dashboard');
  }
}
