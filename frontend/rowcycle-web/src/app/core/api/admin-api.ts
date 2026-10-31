import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_TOAST } from '../http-context';
import { AuditLogEntry, AuditLogQuery, PagedResult, SettingsResponse, UpdateSettingsRequest } from './models';

// ---- API URLs used by this service ----
const SETTINGS = `${environment.apiBaseUrl}/admin/settings`;
const AUDIT_LOGS = `${environment.apiBaseUrl}/admin/audit-logs`;

/** Admin-only settings and audit log. The API returns 403 for anyone else, whatever the UI shows. */
@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);

  getSettings(): Observable<SettingsResponse> {
    return this.http.get<SettingsResponse>(SETTINGS);
  }

  /** The settings form shows validation errors under its fields. */
  updateSettings(request: UpdateSettingsRequest): Observable<SettingsResponse> {
    return this.http.put<SettingsResponse>(SETTINGS, request, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }

  auditLogs(query: AuditLogQuery): Observable<PagedResult<AuditLogEntry>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.entity) {
      params = params.set('entity', query.entity);
    }
    return this.http.get<PagedResult<AuditLogEntry>>(AUDIT_LOGS, { params });
  }
}
