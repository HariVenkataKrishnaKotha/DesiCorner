import { authGuard } from './auth-guard';
import { runGuard } from './guard-test-helpers';

describe('authGuard', () => {
  it('allows an authenticated user', async () => {
    const { result, router } = await runGuard(authGuard, [{ isAuthenticated: true, loading: false }]);

    expect(result).toBeTrue();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('redirects an anonymous user to login, remembering where they were going', async () => {
    const { result, router } = await runGuard(authGuard, [{ isAuthenticated: false, loading: false }], {
      url: '/orders/123',
    });

    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/auth/login'], {
      queryParams: { returnUrl: '/orders/123' },
    });
  });

  it('blocks without redirecting while auth state is loading', async () => {
    const { result, router } = await runGuard(authGuard, [{ isAuthenticated: true, loading: true }]);

    expect(result).toBeFalse();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  // BUG: the guard decides on the first authState$ emission (take(1)). After a page refresh
  // with a stored token, checkAuthSuccess leaves loading=true until the profile request
  // returns, so a logged-in user deep-linking to a protected route has the navigation
  // cancelled (no redirect, blank page). The guard should wait for loading to finish.
  xit('waits for auth loading to finish, then allows the authenticated user', async () => {
    const { result } = await runGuard(authGuard, [
      { isAuthenticated: true, loading: true },
      { isAuthenticated: true, loading: false },
    ]);

    expect(result).toBeTrue();
  });
});
