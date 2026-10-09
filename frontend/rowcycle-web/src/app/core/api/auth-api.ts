import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_AUTH, SKIP_ERROR_TOAST } from '../http-context';
import {
  AuthResponse,
  LoginRequest,
  MeResponse,
  RegisterRequest,
  ResetPasswordRequest,
  VerifyEmailRequest,
} from './api.models';

/** Calls `/api/v1/auth/*` and `/api/v1/me`. Pages show their own errors, so the global error toast is off here. */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/auth';

  register(request: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/register`, request, { context: publicCall() });
  }

  verifyEmail(request: VerifyEmailRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/verify-email`, request, { context: publicCall() });
  }

  /** Sets the refresh-token cookie; `withCredentials` lets the browser store it. */
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login`, request, { context: publicCall(), withCredentials: true });
  }

  /** Sends the refresh-token cookie automatically and gets a new access token + cookie. */
  refresh(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/refresh`, null, { context: publicCall(), withCredentials: true });
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.base}/logout`, null, { context: publicCall(), withCredentials: true });
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${this.base}/forgot-password`, { email }, { context: publicCall() });
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/reset-password`, request, { context: publicCall() });
  }

  me(): Observable<MeResponse> {
    return this.http.get<MeResponse>('/api/v1/me');
  }
}

/** Auth endpoints need no access token, and their pages show errors inline. */
function publicCall(): HttpContext {
  return new HttpContext().set(SKIP_AUTH, true).set(SKIP_ERROR_TOAST, true);
}
