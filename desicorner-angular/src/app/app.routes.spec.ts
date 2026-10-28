import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';
import { routes } from './app.routes';
import { runGuard } from './core/guards/guard-test-helpers';

const SESSION_KEY = 'desicorner_guest_session';

/**
 * The order detail page is where checkout lands, for signed-in users and guests alike.
 * Guests are identified by their guest session; the API decides which order they may read.
 */
describe('orders/:id route access', () => {
  const guard = routes.find(r => r.path === 'orders/:id')!.canActivate![0] as CanActivateFn;

  beforeEach(() => localStorage.removeItem(SESSION_KEY));
  afterEach(() => {
    localStorage.removeItem(SESSION_KEY);
    TestBed.resetTestingModule();
  });

  it('allows a signed-in user', async () => {
    const { result, router } = await runGuard(guard, [{ isAuthenticated: true, loading: false }], { url: '/orders/o1' });

    expect(result).toBeTrue();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('allows a visitor with a guest session (guest checkout lands here)', async () => {
    localStorage.setItem(SESSION_KEY, 'guest-session');

    const { result, router } = await runGuard(guard, [{ isAuthenticated: false, loading: false }], { url: '/orders/o1' });

    expect(result).toBeTrue();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('sends a visitor with no session to login, remembering the order', async () => {
    const { result, router } = await runGuard(guard, [{ isAuthenticated: false, loading: false }], { url: '/orders/o1' });

    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/auth/login'], { queryParams: { returnUrl: '/orders/o1' } });
  });

  it('still requires sign-in for the order list', () => {
    const listGuard = routes.find(r => r.path === 'orders')!.canActivate![0];

    expect(listGuard).not.toBe(guard);
  });
});
