import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { Router, provideRouter } from '@angular/router';
import { MeResponse, Role } from '../api/models';
import { AuthSession } from '../auth/auth-session';

/** Shared setup for the guard specs: a router, a session, and a way to run a guard. */
export function setUpGuardTest() {
  TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
  const session = TestBed.inject(AuthSession);
  const router = TestBed.inject(Router);
  return {
    session,
    router,
    run: <T>(fn: () => T): T => TestBed.runInInjectionContext(fn),
    /** Logs in a fake user with these roles (no API call). */
    logInAs: (...roles: Role[]) => session.user.set(user(roles)),
  };
}

function user(roles: Role[]): MeResponse {
  return { id: 'u1', email: 'a@b.c', emailVerified: true, fullName: 'A', photoUrl: null, phone: null, pointsBalance: 0, roles };
}
