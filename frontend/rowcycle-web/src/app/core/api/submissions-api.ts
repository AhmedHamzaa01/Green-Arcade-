import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_TOAST } from '../http-context';
import {
  AdminSubmissionCategory, CreateSubmissionRequest, PagedResult, SaveSubmissionCategoryRequest, Submission, SubmissionCategory,
  SubmissionStatus,
} from './models';

// ---- API URLs used by these services ----
const CATEGORIES = `${environment.apiBaseUrl}/submission-categories`;
const SUBMISSIONS = `${environment.apiBaseUrl}/submissions`;
const MY_SUBMISSIONS = `${environment.apiBaseUrl}/me/submissions`;
const QUEUE = `${environment.apiBaseUrl}/admin/submissions`;
const queued = (id: string) => `${QUEUE}/${id}`;
const approve = (id: string) => `${queued(id)}/approve`;
const reject = (id: string) => `${queued(id)}/reject`;
const ADMIN_CATEGORIES = `${environment.apiBaseUrl}/admin/submission-categories`;
const adminCategory = (id: string) => `${ADMIN_CATEGORIES}/${id}`;

/** Photo-verified actions: the member side (F3). */
@Injectable({ providedIn: 'root' })
export class SubmissionsApi {
  private readonly http = inject(HttpClient);

  categories(): Observable<SubmissionCategory[]> {
    return this.http.get<SubmissionCategory[]>(CATEGORIES);
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
    return this.http.post<Submission>(SUBMISSIONS, form, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }

  mine(page: number, pageSize: number): Observable<PagedResult<Submission>> {
    return this.http.get<PagedResult<Submission>>(MY_SUBMISSIONS, { params: { page, pageSize } });
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
    return this.http.get<PagedResult<Submission>>(QUEUE, { params });
  }

  get(id: string): Observable<Submission> {
    return this.http.get<Submission>(queued(id));
  }

  approve(id: string): Observable<Submission> {
    return this.http.post<Submission>(approve(id), null);
  }

  reject(id: string, reason: string): Observable<Submission> {
    return this.http.post<Submission>(reject(id), { reason });
  }
}

/** The actions members can submit and their points (Admin). */
@Injectable({ providedIn: 'root' })
export class SubmissionCategoriesAdminApi {
  private readonly http = inject(HttpClient);
  private readonly formCall = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };

  list(): Observable<AdminSubmissionCategory[]> {
    return this.http.get<AdminSubmissionCategory[]>(ADMIN_CATEGORIES);
  }

  // The form shows its own validation errors.
  create(request: SaveSubmissionCategoryRequest): Observable<AdminSubmissionCategory> {
    return this.http.post<AdminSubmissionCategory>(ADMIN_CATEGORIES, request, this.formCall);
  }

  update(id: string, request: SaveSubmissionCategoryRequest): Observable<AdminSubmissionCategory> {
    return this.http.put<AdminSubmissionCategory>(adminCategory(id), request, this.formCall);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(adminCategory(id));
  }
}
