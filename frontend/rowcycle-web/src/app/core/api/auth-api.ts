import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_AUTH, SKIP_ERROR_TOAST } from '../http-context';
import {
  AuthResponse,
  LoginRequest,
  MeResponse,
  RegisterRequest,
  ResetPasswordRequest,
  VerifyEmailRequest,
} from './models';

// ---- API URLs used by this service ----
const AUTH = `${environment.apiBaseUrl}/auth`;
const REGISTER = `${AUTH}/register`;
const VERIFY_EMAIL = `${AUTH}/verify-email`;
const LOGIN = `${AUTH}/login`;
const REFRESH = `${AUTH}/refresh`;
const LOGOUT = `${AUTH}/logout`;
const FORGOT_PASSWORD = `${AUTH}/forgot-password`;
const RESET_PASSWORD = `${AUTH}/reset-password`;
const ME = `${environment.apiBaseUrl}/me`;

/** Sign-up, login and the signed-in user. Pages show their own errors, so the global error toast is off here. */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  register(request: RegisterRequest): Observable<void> {
    return this.http.post<void>(REGISTER, request, { context: publicCall() });
  }

  verifyEmail(request: VerifyEmailRequest): Observable<void> {
    return this.http.post<void>(VERIFY_EMAIL, request, { context: publicCall() });
  }

  /** Sets the refresh-token cookie; `withCredentials` lets the browser store it. */
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(LOGIN, request, { context: publicCall(), withCredentials: true });
  }

  /** Sends the refresh-token cookie automatically and gets a new access token + cookie. */
  refresh(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(REFRESH, null, { context: publicCall(), withCredentials: true });
  }

  logout(): Observable<void> {
    return this.http.post<void>(LOGOUT, null, { context: publicCall(), withCredentials: true });
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(FORGOT_PASSWORD, { email }, { context: publicCall() });
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(RESET_PASSWORD, request, { context: publicCall() });
  }

  me(): Observable<MeResponse> {
    return this.http.get<MeResponse>(ME);
  }
}

/** Auth endpoints need no access token, and their pages show errors inline. */
function publicCall(): HttpContext {
  return new HttpContext().set(SKIP_AUTH, true).set(SKIP_ERROR_TOAST, true);
}
