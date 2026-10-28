import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { Observable, firstValueFrom, from, isObservable, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export interface GuardAuthState {
  isAuthenticated: boolean;
  loading: boolean;
}

/**
 * Starts a functional guard against a stubbed AuthService and Router and returns its
 * outcome as an observable, without waiting for it.
 * `states` is what authState$ emits, in order (to model the store settling).
 */
export function startGuard(
  guard: CanActivateFn,
  states: GuardAuthState[] | Observable<GuardAuthState>,
  options: { isAdmin?: boolean; url?: string } = {},
): { result$: Observable<unknown>; router: jasmine.SpyObj<Router> } {
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
  const result$ = isObservable(outcome) ? outcome : from(Promise.resolve(outcome));
  return { result$, router };
}

/** Runs a guard to completion and returns its decision. */
export async function runGuard(
  guard: CanActivateFn,
  states: GuardAuthState[] | Observable<GuardAuthState>,
  options: { isAdmin?: boolean; url?: string } = {},
): Promise<{ result: unknown; router: jasmine.SpyObj<Router> }> {
  const { result$, router } = startGuard(guard, states, options);
  return { result: await firstValueFrom(result$), router };
}
