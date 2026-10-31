import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_TOAST } from '../http-context';
import { AdminUserDetail, AdminUserListItem, Dashboard, PagedResult, PointsChange, PointsHistoryResponse, Role, UserQuery } from './models';

// ---- API URLs used by these services ----
const USERS = `${environment.apiBaseUrl}/admin/users`;
const user = (id: string) => `${USERS}/${id}`;
const userPoints = (id: string) => `${user(id)}/points`;
const userRoles = (id: string) => `${user(id)}/roles`;
const userBlock = (id: string) => `${user(id)}/block`;
const userAdjustments = (id: string) => `${user(id)}/points-adjustments`;
const DASHBOARD = `${environment.apiBaseUrl}/admin/dashboard`;

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
    return this.http.get<PagedResult<AdminUserListItem>>(USERS, { params });
  }

  get(id: string): Observable<AdminUserDetail> {
    return this.http.get<AdminUserDetail>(user(id));
  }

  points(id: string, page: number, pageSize: number): Observable<PointsHistoryResponse> {
    return this.http.get<PointsHistoryResponse>(userPoints(id), { params: { page, pageSize } });
  }

  setRoles(id: string, roles: Role[]): Observable<AdminUserDetail> {
    return this.http.put<AdminUserDetail>(userRoles(id), { roles });
  }

  setBlocked(id: string, blocked: boolean): Observable<AdminUserDetail> {
    return this.http.put<AdminUserDetail>(userBlock(id), { blocked });
  }

  /** The dialog shows the API's errors itself (e.g. "Not enough points."). */
  adjustPoints(id: string, amount: number, reason: string): Observable<PointsChange> {
    return this.http.post<PointsChange>(userAdjustments(id), { amount, reason }, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }
}

/** The staff home screen (Moderator, StoreManager, Admin). */
@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);

  get(): Observable<Dashboard> {
    return this.http.get<Dashboard>(DASHBOARD);
  }
}
