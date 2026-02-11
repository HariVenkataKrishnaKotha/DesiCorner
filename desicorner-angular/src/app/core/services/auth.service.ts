import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { Store } from '@ngrx/store';
import { Actions, ofType } from '@ngrx/effects';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { AppState } from '../../store';
import { AuthActions } from '../../store/auth/auth.actions';
import {
  selectIsAuthenticated,
  selectUserProfile,
  selectAuthStateForGuards,
} from '../../store/auth/auth.selectors';
import { GuestSessionService } from './guest-session.service';
import {
  RegisterRequest,
  UserProfile,
  SendOtpRequest,
  VerifyOtpRequest,
} from '../models/auth.models';
import { ApiResponse } from '../models/response.models';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private store = inject(Store<AppState>);
  private actions$ = inject(Actions);
  private oauthService = inject(OAuthService);
  private guestSessionService = inject(GuestSessionService);

  /** Observable for guards and components */
  public authState$ = this.store.select(selectAuthStateForGuards);

  constructor() {
    this.configureOAuth();
    this.store.dispatch(AuthActions.checkAuth());
  }

  /** Configure angular-oauth2-oidc for Authorization Code + PKCE */
  private configureOAuth(): void {
    const authConfig: AuthConfig = {
      issuer: environment.oidc.issuer,
      clientId: environment.oidc.clientId,
      redirectUri: environment.oidc.redirectUri,
      postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
      responseType: environment.oidc.responseType,
      scope: environment.oidc.scope,
      showDebugInformation: environment.oidc.showDebugInformation,
      requireHttps: environment.oidc.requireHttps,
      strictDiscoveryDocumentValidation: environment.oidc.strictDiscoveryDocumentValidation,
    };

    this.oauthService.configure(authConfig);
    this.oauthService.setupAutomaticSilentRefresh();

    // Only auto-process tokens on non-callback routes.
    // The callback component handles the auth code exchange exclusively
    // to avoid a double exchange (which causes a 400 error).
    if (!window.location.pathname.includes('/auth/callback')) {
      this.oauthService.loadDiscoveryDocumentAndTryLogin().then(loggedIn => {
        if (loggedIn && this.oauthService.hasValidAccessToken()) {
          this.store.dispatch(AuthActions.pkceCallbackSuccess());
        }
      });
    }
  }

  /** Initiate PKCE login — redirects to AuthServer */
  initLogin(): void {
    this.oauthService.initCodeFlow();
  }

  /**
   * Handle PKCE callback (called from callback component).
   * Returns true if tokens were received successfully.
   */
  async handlePkceCallback(): Promise<boolean> {
    try {
      const loggedIn = await this.oauthService.loadDiscoveryDocumentAndTryLogin();
      if (loggedIn && this.oauthService.hasValidAccessToken()) {
        this.store.dispatch(AuthActions.pkceCallbackSuccess());
        return true;
      }
      return false;
    } catch {
      return false;
    }
  }

  /** Trigger profile reload */
  loadUserProfile(): void {
    this.store.dispatch(AuthActions.loadUserProfile());
  }

  /** Logout — clear local state, then redirect to AuthServer to clear the cookie */
  logout(): void {
    // Clear local NgRx state + localStorage tokens first
    this.store.dispatch(AuthActions.logout());

    // Redirect to AuthServer logout to clear the .DesiCorner.Auth cookie.
    // Without this, the next login would auto-authenticate as the previous user.
    const postLogoutUri = encodeURIComponent(environment.oidc.postLogoutRedirectUri);
    window.location.href = `${environment.authServerUrl}/Account/Logout?post_logout_redirect_uri=${postLogoutUri}`;
  }

  /** Register new user */
  register(request: RegisterRequest): Observable<ApiResponse> {
    this.store.dispatch(AuthActions.register({ request }));
    return this.actions$.pipe(
      ofType(AuthActions.registerSuccess, AuthActions.registerFailure),
      take(1),
      map(action => {
        if (action.type === AuthActions.registerSuccess.type) {
          return (action as any).response as ApiResponse;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse;
      })
    );
  }

  /** Send OTP */
  sendOtp(request: SendOtpRequest): Observable<ApiResponse> {
    this.store.dispatch(AuthActions.sendOtp({ request }));
    return this.actions$.pipe(
      ofType(AuthActions.sendOtpSuccess, AuthActions.sendOtpFailure),
      take(1),
      map(action => {
        if (action.type === AuthActions.sendOtpSuccess.type) {
          return (action as any).response as ApiResponse;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse;
      })
    );
  }

  /** Verify OTP */
  verifyOtp(request: VerifyOtpRequest): Observable<ApiResponse> {
    this.store.dispatch(AuthActions.verifyOtp({ request }));
    return this.actions$.pipe(
      ofType(AuthActions.verifyOtpSuccess, AuthActions.verifyOtpFailure),
      take(1),
      map(action => {
        if (action.type === AuthActions.verifyOtpSuccess.type) {
          return (action as any).response as ApiResponse;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse;
      })
    );
  }

  // --- Synchronous getters ---

  get isAuthenticated(): boolean {
    // Check both NgRx store and OAuthService
    if (this.oauthService.hasValidAccessToken()) {
      return true;
    }
    let value = false;
    this.store.select(selectIsAuthenticated).pipe(take(1)).subscribe(v => value = v);
    return value;
  }

  get currentUser(): UserProfile | undefined {
    let value: UserProfile | undefined;
    this.store.select(selectUserProfile).pipe(take(1)).subscribe(v => value = v);
    return value;
  }

  get accessToken(): string | null {
    return this.oauthService.getAccessToken() || localStorage.getItem('access_token');
  }

  getToken(): string | null {
    return this.accessToken;
  }

  hasRole(role: string): boolean {
    return this.currentUser?.roles?.includes(role) ?? false;
  }

  isAdmin(): boolean {
    return this.hasRole('Admin');
  }

  get guestSessionId(): string {
    return this.guestSessionService.getSessionId();
  }

  get isGuest(): boolean {
    return !this.isAuthenticated && this.guestSessionService.hasSession();
  }

  clearGuestSession(): void {
    this.guestSessionService.clearSession();
  }
}
