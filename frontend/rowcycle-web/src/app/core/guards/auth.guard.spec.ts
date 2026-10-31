import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';
import { setUpGuardTest } from './guard-testing';

describe('authGuard', () => {
  const state = { url: '/account/points' } as RouterStateSnapshot;
  const route = {} as ActivatedRouteSnapshot;

  it('sends logged-out users to login with a return URL', () => {
    const { run, router } = setUpGuardTest();

    const result = run(() => authGuard(route, state)) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/login?returnUrl=%2Faccount%2Fpoints');
  });

  it('lets logged-in users through', () => {
    const { run, logInAs } = setUpGuardTest();
    logInAs('Member');

    expect(run(() => authGuard(route, state))).toBe(true);
  });
});
