import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthSession } from './auth-session';

describe('AuthSession', () => {
  let session: AuthSession;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    session = TestBed.inject(AuthSession);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('restore() logs back in when the refresh cookie is valid', async () => {
    const done = session.restore();
    httpMock.expectOne('/api/v1/auth/refresh').flush({ accessToken: 't', accessTokenExpiresAt: '' });
    (await vi.waitFor(() => httpMock.expectOne('/api/v1/me'))).flush({ id: 'u1', roles: ['Member'] });
    await done;

    expect(session.isLoggedIn()).toBe(true);
    expect(session.token()).toBe('t');
    expect(session.hasRole('Member')).toBe(true);
    expect(session.hasRole('Admin')).toBe(false);
  });

  it('restore() stays logged out, without throwing, when there is no valid cookie', async () => {
    const done = session.restore();
    httpMock.expectOne('/api/v1/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
    await done;

    expect(session.isLoggedIn()).toBe(false);
    expect(session.token()).toBeNull();
  });

  it('logout() clears the session even if the server call fails', async () => {
    session.user.set({ id: 'u1', email: '', emailVerified: true, fullName: '', photoUrl: null, phone: null, pointsBalance: 0, roles: ['Member'] });

    const done = session.logout();
    httpMock.expectOne('/api/v1/auth/logout').flush(null, { status: 500, statusText: 'Error' });

    await expect(done).rejects.toBeDefined();
    expect(session.isLoggedIn()).toBe(false);
  });
});
