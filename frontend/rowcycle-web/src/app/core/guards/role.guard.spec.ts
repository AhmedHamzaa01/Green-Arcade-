import { UrlSegment, UrlTree } from '@angular/router';
import { roleGuard } from './role.guard';
import { setUpGuardTest } from './guard-testing';

describe('roleGuard', () => {
  it('lets admins in and sends members home', () => {
    const { run, router, logInAs } = setUpGuardTest();
    const segments = [new UrlSegment('admin', {}), new UrlSegment('settings', {})];

    logInAs('Admin');
    expect(run(() => roleGuard('Admin')({}, segments, {} as never))).toBe(true);

    logInAs('Member');
    expect(router.serializeUrl(run(() => roleGuard('Admin')({}, segments, {} as never)) as UrlTree)).toBe('/');
  });

  it('sends logged-out users to login', () => {
    const { run, router } = setUpGuardTest();
    const segments = [new UrlSegment('admin', {})];

    const result = run(() => roleGuard('Admin')({}, segments, {} as never)) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/login?returnUrl=%2Fadmin');
  });
});
