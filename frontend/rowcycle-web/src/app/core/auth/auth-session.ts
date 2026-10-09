import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, finalize, firstValueFrom, map, shareReplay } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { LoginRequest, MeResponse, Role } from '../api/api.models';

/**
 * Who is logged in. The access token is kept in memory only (never in localStorage);
 * the refresh token is an httpOnly cookie that only the browser and the API can see.
 */
@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly api = inject(AuthApi);
  private readonly accessToken = signal<string | null>(null);
  private refreshInFlight: Observable<string> | null = null;

  /** The signed-in user (from `GET /me`), or null. */
  readonly user = signal<MeResponse | null>(null);
  readonly isLoggedIn = computed(() => this.user() !== null);
  readonly roles = computed<Role[]>(() => this.user()?.roles ?? []);

  token(): string | null {
    return this.accessToken();
  }

  hasRole(...roles: Role[]): boolean {
    return this.roles().some((role) => roles.includes(role));
  }

  async login(request: LoginRequest): Promise<void> {
    const response = await firstValueFrom(this.api.login(request));
    this.accessToken.set(response.accessToken);
    await this.reloadUser();
  }

  /**
   * Called once when the app starts: if the refresh cookie is still valid, the user is logged in again.
   * This is what keeps you logged in after reloading the page. Never throws.
   */
  async restore(): Promise<void> {
    try {
      await firstValueFrom(this.refreshAccessToken());
      await this.reloadUser();
    } catch {
      this.clear();
    }
  }

  /**
   * Gets a new access token using the cookie. If several requests need it at once,
   * they all share this one call, because each refresh token works only once.
   */
  refreshAccessToken(): Observable<string> {
    this.refreshInFlight ??= this.api.refresh().pipe(
      map((response) => {
        this.accessToken.set(response.accessToken);
        return response.accessToken;
      }),
      finalize(() => (this.refreshInFlight = null)),
      shareReplay(1),
    );
    return this.refreshInFlight;
  }

  async reloadUser(): Promise<void> {
    this.user.set(await firstValueFrom(this.api.me()));
  }

  /** Ends the session on the server (revokes the cookie) and forgets the user here. */
  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.api.logout());
    } finally {
      this.clear();
    }
  }

  clear(): void {
    this.accessToken.set(null);
    this.user.set(null);
  }
}
