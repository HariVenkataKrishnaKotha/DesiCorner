import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { HttpClient } from '@angular/common/http';
import { switchMap, map, catchError, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { AuthActions } from './auth.actions';
import { GuestSessionService } from '../../core/services/guest-session.service';
import { environment } from '@env/environment';
import { ApiResponse } from '../../core/models/response.models';
import { UserProfile } from '../../core/models/auth.models';

@Injectable()
export class AuthEffects {
  private actions$ = inject(Actions);
  private http = inject(HttpClient);
  private router = inject(Router);
  private guestSessionService = inject(GuestSessionService);

  private readonly USER_KEY = 'desicorner_user';

  /** Check auth on app init — checks for valid access token in localStorage */
  checkAuth$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.checkAuth),
      map(() => {
        const token = localStorage.getItem('access_token');
        if (token) {
          return AuthActions.checkAuthSuccess();
        }
        return AuthActions.checkAuthNoToken();
      })
    )
  );

  /** After checkAuthSuccess, load profile */
  checkAuthLoadProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.checkAuthSuccess),
      map(() => AuthActions.loadUserProfile())
    )
  );

  /** After PKCE callback success, load user profile */
  pkceCallbackLoadProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.pkceCallbackSuccess),
      map(() => AuthActions.loadUserProfile())
    )
  );

  /** Load user profile from API */
  loadUserProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.loadUserProfile),
      switchMap(() =>
        this.http.get<ApiResponse<UserProfile>>(
          `${environment.gatewayUrl}/api/account/profile`
        ).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              localStorage.setItem(this.USER_KEY, JSON.stringify(response.result));
              return AuthActions.loadUserProfileSuccess({ profile: response.result });
            }
            return AuthActions.loadUserProfileFailure({ error: 'Failed to load profile' });
          }),
          catchError(error => {
            const message = error.error?.message || 'Failed to load profile';
            return of(AuthActions.loadUserProfileFailure({ error: message }));
          })
        )
      )
    )
  );

  /** Profile load failure triggers logout (clear stale tokens) */
  profileFailureLogout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.loadUserProfileFailure),
      map(() => AuthActions.logout())
    )
  );

  /** Logout — clear all tokens from localStorage and navigate */
  logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.logout),
      tap(() => {
        // Clear OAuth tokens (stored by angular-oauth2-oidc)
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('id_token');
        localStorage.removeItem('granted_scopes');
        localStorage.removeItem('access_token_stored_at');
        localStorage.removeItem('id_token_stored_at');
        localStorage.removeItem('id_token_expires_at');
        localStorage.removeItem('id_token_claims_obj');
        localStorage.removeItem('nonce');
        localStorage.removeItem('PKCE_verifier');
        localStorage.removeItem('session_state');
        // Clear legacy keys
        localStorage.removeItem('token_expiry');
        localStorage.removeItem(this.USER_KEY);
        localStorage.removeItem('guest_session_id');
        this.guestSessionService.clearSession();
        this.router.navigate(['/']);
      })
    ),
    { dispatch: false }
  );

  /** Register */
  register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.register),
      switchMap(({ request }) =>
        this.http.post<ApiResponse>(
          `${environment.gatewayUrl}/api/account/register`,
          request
        ).pipe(
          map(response => AuthActions.registerSuccess({ response })),
          catchError(error => {
            const message = error.error?.message || 'Registration failed';
            return of(AuthActions.registerFailure({ error: message }));
          })
        )
      )
    )
  );

  /** Send OTP */
  sendOtp$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.sendOtp),
      switchMap(({ request }) =>
        this.http.post<ApiResponse>(
          `${environment.gatewayUrl}/api/account/send-otp`,
          request
        ).pipe(
          map(response => AuthActions.sendOtpSuccess({ response })),
          catchError(error => {
            const message = error.error?.message || 'Failed to send OTP';
            return of(AuthActions.sendOtpFailure({ error: message }));
          })
        )
      )
    )
  );

  /** Verify OTP */
  verifyOtp$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.verifyOtp),
      switchMap(({ request }) =>
        this.http.post<ApiResponse>(
          `${environment.gatewayUrl}/api/account/verify-otp`,
          request
        ).pipe(
          map(response => AuthActions.verifyOtpSuccess({ response })),
          catchError(error => {
            const message = error.error?.message || 'OTP verification failed';
            return of(AuthActions.verifyOtpFailure({ error: message }));
          })
        )
      )
    )
  );
}
