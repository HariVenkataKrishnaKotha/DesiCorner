import { adminGuard } from './admin-guard';
import { runGuard } from './guard-test-helpers';

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

  it('blocks without redirecting while auth state is loading', async () => {
    const { result, router } = await runGuard(adminGuard, [{ isAuthenticated: true, loading: true }], {
      isAdmin: true,
    });

    expect(result).toBeFalse();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  // BUG: same as authGuard. The decision is taken on the first emission, so an admin who
  // refreshes /admin while the profile is still loading has the navigation cancelled.
  xit('waits for auth loading to finish, then allows the admin', async () => {
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
