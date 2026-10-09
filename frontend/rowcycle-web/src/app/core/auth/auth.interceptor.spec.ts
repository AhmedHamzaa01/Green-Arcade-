import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MeResponse } from '../api/api.models';
import { authInterceptor } from './auth.interceptor';
import { AuthSession } from './auth-session';

const member: MeResponse = {
  id: 'u1', email: 'sara@example.com', emailVerified: true, fullName: 'Sara',
  photoUrl: null, phone: null, pointsBalance: 0, roles: ['Member'],
};

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let session: AuthSession;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSession);
  });

  afterEach(() => httpMock.verify());

  /** Logs in through the real AuthSession, answering the login and /me calls. */
  async function loginWithToken(token: string): Promise<void> {
    const done = session.login({ email: member.email, password: 'Secret123' });
    const login = httpMock.expectOne('/api/v1/auth/login');
    expect(login.request.headers.has('Authorization')).toBe(false);
    expect(login.request.withCredentials).toBe(true);
    login.flush({ accessToken: token, accessTokenExpiresAt: '' });
    (await vi.waitFor(() => httpMock.expectOne('/api/v1/me'))).flush(member);
    await done;
  }

  it('adds the access token to API calls', async () => {
    await loginWithToken('t1');

    const result = firstValueFrom(http.get('/api/v1/me/points'));
    const request = httpMock.expectOne('/api/v1/me/points');

    expect(request.request.headers.get('Authorization')).toBe('Bearer t1');
    request.flush({});
    await result;
  });

  it('does not add a token to non-API requests', () => {
    http.get('/i18n/en.json').subscribe();

    expect(httpMock.expectOne('/i18n/en.json').request.headers.has('Authorization')).toBe(false);
  });

  it('refreshes once on 401 and retries with the new token', async () => {
    await loginWithToken('expired');

    const result = firstValueFrom(http.get<{ ok: boolean }>('/api/v1/me/points'));
    httpMock.expectOne('/api/v1/me/points').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/v1/auth/refresh').flush({ accessToken: 'fresh', accessTokenExpiresAt: '' });
    const retry = httpMock.expectOne('/api/v1/me/points');

    expect(retry.request.headers.get('Authorization')).toBe('Bearer fresh');
    retry.flush({ ok: true });
    expect(await result).toEqual({ ok: true });
  });

  it('shares one refresh between requests that fail at the same time', async () => {
    await loginWithToken('expired');

    const first = firstValueFrom(http.get('/api/v1/me/points'));
    const second = firstValueFrom(http.get('/api/v1/me'));
    httpMock.expectOne('/api/v1/me/points').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/v1/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    // Only one refresh call, because each refresh token works once.
    httpMock.expectOne('/api/v1/auth/refresh').flush({ accessToken: 'fresh', accessTokenExpiresAt: '' });
    httpMock.expectOne('/api/v1/me/points').flush({});
    httpMock.expectOne('/api/v1/me').flush(member);
    await Promise.all([first, second]);
  });

  it('logs out and goes to the login page when the refresh fails', async () => {
    await loginWithToken('expired');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    const result = firstValueFrom(http.get('/api/v1/me/points'));
    httpMock.expectOne('/api/v1/me/points').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/v1/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(result).rejects.toBeInstanceOf(HttpErrorResponse);
    expect(session.isLoggedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.objectContaining({ queryParams: expect.any(Object) }));
  });

  it('passes other errors through without refreshing', async () => {
    await loginWithToken('t1');

    const result = firstValueFrom(http.get('/api/v1/admin/settings'));
    httpMock.expectOne('/api/v1/admin/settings').flush(null, { status: 403, statusText: 'Forbidden' });

    await expect(result).rejects.toMatchObject({ status: 403 });
    httpMock.expectNone('/api/v1/auth/refresh');
  });
});
