import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { filter, map, take } from 'rxjs/operators';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Wait until the auth check / profile load has finished. Every outcome of that load
  // (success, failure, no token, logout) sets loading to false, so this cannot hang.
  return authService.authState$.pipe(
    filter(authState => !authState.loading),
    take(1),
    map(authState => {
      if (authState.isAuthenticated) {
        return true;
      }

      // Store the attempted URL for redirecting
      router.navigate(['/auth/login'], {
        queryParams: { returnUrl: state.url }
      });
      return false;
    })
  );
};