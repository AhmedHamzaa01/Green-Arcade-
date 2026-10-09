import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlSegment, UrlTree, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { MeResponse, Role } from '../api/api.models';
import { authGuard, guestGuard, roleGuard } from './auth.guards';
import { AuthSession } from './auth-session';

function user(roles: Role[]): MeResponse {
  return { id: 'u1', email: 'a@b.c', emailVerified: true, fullName: 'A', photoUrl: null, phone: null, pointsBalance: 0, roles };
}

describe('auth guards', () => {
  let session: AuthSession;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
    session = TestBed.inject(AuthSession);
    router = TestBed.inject(Router);
  });

  const run = <T>(fn: () => T): T => TestBed.runInInjectionContext(fn);
  const state = { url: '/account/points' } as RouterStateSnapshot;
  const route = {} as ActivatedRouteSnapshot;

  it('authGuard sends logged-out users to login with a return URL', () => {
    const result = run(() => authGuard(route, state)) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/login?returnUrl=%2Faccount%2Fpoints');
  });

  it('authGuard lets logged-in users through', () => {
    session.user.set(user(['Member']));

    expect(run(() => authGuard(route, state))).toBe(true);
  });

  it('guestGuard sends logged-in users to their profile', () => {
    session.user.set(user(['Member']));

    expect(router.serializeUrl(run(() => guestGuard(route, state)) as UrlTree)).toBe('/account/profile');
  });

  it('roleGuard lets admins in and sends members home', () => {
    const segments = [new UrlSegment('admin', {}), new UrlSegment('settings', {})];

    session.user.set(user(['Admin']));
    expect(run(() => roleGuard('Admin')({}, segments, {} as never))).toBe(true);

    session.user.set(user(['Member']));
    expect(router.serializeUrl(run(() => roleGuard('Admin')({}, segments, {} as never)) as UrlTree)).toBe('/');
  });

  it('roleGuard sends logged-out users to login', () => {
    const segments = [new UrlSegment('admin', {})];

    const result = run(() => roleGuard('Admin')({}, segments, {} as never)) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/login?returnUrl=%2Fadmin');
  });
});
