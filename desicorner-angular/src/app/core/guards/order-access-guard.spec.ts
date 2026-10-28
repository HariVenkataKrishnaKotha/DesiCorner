import { Subject } from 'rxjs';
import { orderAccessGuard } from './order-access-guard';
import { GuardAuthState, runGuard } from './guard-test-helpers';

const SESSION_KEY = 'desicorner_guest_session';

describe('orderAccessGuard', () => {
  beforeEach(() => localStorage.removeItem(SESSION_KEY));
  afterEach(() => localStorage.removeItem(SESSION_KEY));

  it('allows a signed-in user', async () => {
    const { result } = await runGuard(orderAccessGuard, [{ isAuthenticated: true, loading: false }]);

    expect(result).toBeTrue();
  });

  it('allows a visitor with a guest session', async () => {
    localStorage.setItem(SESSION_KEY, 'guest-session');

    const { result, router } = await runGuard(orderAccessGuard, [{ isAuthenticated: false, loading: false }]);

    expect(result).toBeTrue();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('sends a visitor without a session to login with returnUrl', async () => {
    const { result, router } = await runGuard(orderAccessGuard, [{ isAuthenticated: false, loading: false }], {
      url: '/orders/o1',
    });

    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/auth/login'], { queryParams: { returnUrl: '/orders/o1' } });
  });

  it('waits for the auth check to settle before deciding', async () => {
    const states = new Subject<GuardAuthState>();
    const pending = runGuard(orderAccessGuard, states);

    states.next({ isAuthenticated: true, loading: true });
    states.next({ isAuthenticated: true, loading: false });
    const { result, router } = await pending;

    expect(result).toBeTrue();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
