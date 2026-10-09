import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PointsHistoryResponse } from './api.models';

@Injectable({ providedIn: 'root' })
export class PointsApi {
  private readonly http = inject(HttpClient);

  /** Balance + one page of the ledger, newest first. */
  history(page: number, pageSize: number): Observable<PointsHistoryResponse> {
    return this.http.get<PointsHistoryResponse>('/api/v1/me/points', { params: { page, pageSize } });
  }
}
