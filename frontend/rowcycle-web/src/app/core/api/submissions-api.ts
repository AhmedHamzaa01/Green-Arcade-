import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_ERROR_TOAST } from '../http-context';
import { CreateSubmissionRequest, PagedResult, Submission, SubmissionCategory, SubmissionStatus } from './api.models';

/** Photo-verified actions: the member side (F3). */
@Injectable({ providedIn: 'root' })
export class SubmissionsApi {
  private readonly http = inject(HttpClient);

  categories(): Observable<SubmissionCategory[]> {
    return this.http.get<SubmissionCategory[]>('/api/v1/submission-categories');
  }

  /** Multipart upload. The form shows the API's errors itself (e.g. daily limit reached). */
  create(request: CreateSubmissionRequest, photo: File): Observable<Submission> {
    const form = new FormData();
    form.append('photo', photo);
    form.append('categoryId', request.categoryId);
    if (request.note) {
      form.append('note', request.note);
    }
    if (request.weightKg !== null) {
      form.append('weightKg', String(request.weightKg));
    }
    return this.http.post<Submission>('/api/v1/submissions', form, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }

  mine(page: number, pageSize: number): Observable<PagedResult<Submission>> {
    return this.http.get<PagedResult<Submission>>('/api/v1/me/submissions', { params: { page, pageSize } });
  }
}

/** The moderation queue (Moderator, Admin). */
@Injectable({ providedIn: 'root' })
export class ModerationApi {
  private readonly http = inject(HttpClient);

  search(query: { status?: SubmissionStatus | ''; page: number; pageSize: number }): Observable<PagedResult<Submission>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.status) {
      params = params.set('status', query.status);
    }
    return this.http.get<PagedResult<Submission>>('/api/v1/admin/submissions', { params });
  }

  get(id: string): Observable<Submission> {
    return this.http.get<Submission>(`/api/v1/admin/submissions/${id}`);
  }

  approve(id: string): Observable<Submission> {
    return this.http.post<Submission>(`/api/v1/admin/submissions/${id}/approve`, null);
  }

  reject(id: string, reason: string): Observable<Submission> {
    return this.http.post<Submission>(`/api/v1/admin/submissions/${id}/reject`, { reason });
  }
}
