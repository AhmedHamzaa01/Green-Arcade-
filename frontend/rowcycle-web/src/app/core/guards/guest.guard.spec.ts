import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { guestGuard } from './guest.guard';
import { setUpGuardTest } from './guard-testing';

describe('guestGuard', () => {
  const state = { url: '/login' } as RouterStateSnapshot;
  const route = {} as ActivatedRouteSnapshot;

  it('sends logged-in users to their profile', () => {
    const { run, router, logInAs } = setUpGuardTest();
    logInAs('Member');

    expect(router.serializeUrl(run(() => guestGuard(route, state)) as UrlTree)).toBe('/account/profile');
  });

  it('lets visitors open the page', () => {
    const { run } = setUpGuardTest();

    expect(run(() => guestGuard(route, state))).toBe(true);
  });
});
