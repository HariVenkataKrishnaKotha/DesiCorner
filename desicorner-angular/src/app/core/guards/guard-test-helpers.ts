import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { Observable, firstValueFrom, isObservable, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export interface GuardAuthState {
  isAuthenticated: boolean;
  loading: boolean;
}

/**
 * Runs a functional guard against a stubbed AuthService and Router.
 * `states` is what authState$ emits, in order (to model the store settling).
 */
export async function runGuard(
  guard: CanActivateFn,
  states: GuardAuthState[] | Observable<GuardAuthState>,
  options: { isAdmin?: boolean; url?: string } = {},
): Promise<{ result: unknown; router: jasmine.SpyObj<Router> }> {
  const router = jasmine.createSpyObj<Router>('Router', ['navigate']);
  const authState$ = isObservable(states) ? states : of(...states);
  const authService = { authState$, isAdmin: () => options.isAdmin ?? false };

  TestBed.configureTestingModule({
    providers: [
      { provide: AuthService, useValue: authService },
      { provide: Router, useValue: router },
    ],
  });

  const state = { url: options.url ?? '/orders' } as RouterStateSnapshot;
  const outcome = TestBed.runInInjectionContext(() => guard({} as ActivatedRouteSnapshot, state));
  const result = isObservable(outcome) ? await firstValueFrom(outcome) : await outcome;
  return { result, router };
}
