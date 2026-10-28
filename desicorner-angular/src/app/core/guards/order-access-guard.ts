import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { GuestSessionService } from '../services/guest-session.service';
import { filter, map, take } from 'rxjs/operators';

/**
 * Order detail is where checkout lands, for signed-in users and guests alike. Allow a signed-in
 * user, or a visitor with a guest session (desicorner_guest_session). The API decides whether
 * that caller may read the specific order. Anyone else is sent to login.
 */
export const orderAccessGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const guestSessionService = inject(GuestSessionService);
  const router = inject(Router);

  // Wait for the auth check to settle, as authGuard does
  return authService.authState$.pipe(
    filter(authState => !authState.loading),
    take(1),
    map(authState => {
      if (authState.isAuthenticated || guestSessionService.hasSession()) {
        return true;
      }

      router.navigate(['/auth/login'], {
        queryParams: { returnUrl: state.url }
      });
      return false;
    })
  );
};
