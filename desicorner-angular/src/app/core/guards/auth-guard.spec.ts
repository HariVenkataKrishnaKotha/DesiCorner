import { authGuard } from './auth-guard';
import { BehaviorSubject } from 'rxjs';
import { GuardAuthState, runGuard, startGuard } from './guard-test-helpers';

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

  it('makes no decision and no redirect while auth state is still loading', () => {
    const authState$ = new BehaviorSubject<GuardAuthState>({ isAuthenticated: true, loading: true });
    const { result$, router } = startGuard(authGuard, authState$);
    const results: unknown[] = [];

    result$.subscribe(r => results.push(r));

    expect(results).toEqual([]);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('denies once loading ends in failure (profile could not be loaded)', async () => {
    const { result, router } = await runGuard(authGuard, [
      { isAuthenticated: true, loading: true },
      { isAuthenticated: false, loading: false },
    ]);

    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/auth/login'], { queryParams: { returnUrl: '/orders' } });
  });

  // Regression: the guard used take(1) on the first emission, so a refresh with a stored
  // token (loading=true until the profile returns) cancelled the navigation.
  it('waits for auth loading to finish, then allows the authenticated user', async () => {
    const { result } = await runGuard(authGuard, [
      { isAuthenticated: true, loading: true },
      { isAuthenticated: true, loading: false },
    ]);

    expect(result).toBeTrue();
  });
});
