import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PointsHistoryResponse } from './models';

// ---- API URLs used by this service ----
const MY_POINTS = `${environment.apiBaseUrl}/me/points`;

/** The signed-in member's points (F2). */
@Injectable({ providedIn: 'root' })
export class PointsApi {
  private readonly http = inject(HttpClient);

  /** Balance + one page of the ledger, newest first. */
  history(page: number, pageSize: number): Observable<PointsHistoryResponse> {
    return this.http.get<PointsHistoryResponse>(MY_POINTS, { params: { page, pageSize } });
  }
}
