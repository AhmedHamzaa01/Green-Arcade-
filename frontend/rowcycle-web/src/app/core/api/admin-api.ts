import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_ERROR_TOAST } from '../http-context';
import { AuditLogEntry, AuditLogQuery, PagedResult, SettingsResponse, UpdateSettingsRequest } from './api.models';

/** Admin-only endpoints. The API returns 403 for anyone else, whatever the UI shows. */
@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);

  getSettings(): Observable<SettingsResponse> {
    return this.http.get<SettingsResponse>('/api/v1/admin/settings');
  }

  /** The settings form shows validation errors under its fields. */
  updateSettings(request: UpdateSettingsRequest): Observable<SettingsResponse> {
    return this.http.put<SettingsResponse>('/api/v1/admin/settings', request, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }

  auditLogs(query: AuditLogQuery): Observable<PagedResult<AuditLogEntry>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.entity) {
      params = params.set('entity', query.entity);
    }
    return this.http.get<PagedResult<AuditLogEntry>>('/api/v1/admin/audit-logs', { params });
  }
}
