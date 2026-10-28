import { adminGuard } from './admin-guard';
import { BehaviorSubject } from 'rxjs';
import { GuardAuthState, runGuard, startGuard } from './guard-test-helpers';

describe('adminGuard', () => {
  it('allows an authenticated admin', async () => {
    const { result, router } = await runGuard(adminGuard, [{ isAuthenticated: true, loading: false }], {
      isAdmin: true,
    });

    expect(result).toBeTrue();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('sends an authenticated non-admin home', async () => {
    const { result, router } = await runGuard(adminGuard, [{ isAuthenticated: true, loading: false }], {
      isAdmin: false,
    });

    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/']);
  });

  it('sends an anonymous user home, even if a stale admin role is reported', async () => {
    const { result, router } = await runGuard(adminGuard, [{ isAuthenticated: false, loading: false }], {
      isAdmin: true,
    });

    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/']);
  });

  it('makes no decision and no redirect while auth state is still loading', () => {
    const authState$ = new BehaviorSubject<GuardAuthState>({ isAuthenticated: true, loading: true });
    const { result$, router } = startGuard(adminGuard, authState$, { isAdmin: true });
    const results: unknown[] = [];

    result$.subscribe(r => results.push(r));

    expect(results).toEqual([]);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('denies once loading ends in failure (profile could not be loaded)', async () => {
    const { result, router } = await runGuard(adminGuard, [
      { isAuthenticated: true, loading: true },
      { isAuthenticated: false, loading: false },
    ], { isAdmin: true });

    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/']);
  });

  // Regression: the guard used take(1) on the first emission, so a refresh with a stored
  // token (loading=true until the profile returns) cancelled the navigation.
  it('waits for auth loading to finish, then allows the admin', async () => {
    const { result } = await runGuard(
      adminGuard,
      [
        { isAuthenticated: true, loading: true },
        { isAuthenticated: true, loading: false },
      ],
      { isAdmin: true },
    );

    expect(result).toBeTrue();
  });
});
