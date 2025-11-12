import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map, take } from 'rxjs/operators';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.authState$.pipe(
    take(1),
    map(authState => {
      if (authState.loading) {
        return false;
      }

      if (authState.isAuthenticated && authService.isAdmin()) {
        return true;
      }

      // Redirect to home if not admin
      router.navigate(['/']);
      return false;
    })
  );
};