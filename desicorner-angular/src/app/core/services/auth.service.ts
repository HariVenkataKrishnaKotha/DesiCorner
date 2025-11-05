import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap, catchError, of } from 'rxjs';
import { OAuthService, OAuthEvent, OAuthErrorEvent } from 'angular-oauth2-oidc';
import { environment } from '@env/environment';
import { 
  LoginRequest, 
  RegisterRequest, 
  UserProfile, 
  AuthState,
  SendOtpRequest,
  VerifyOtpRequest 
} from '../models/auth.models';
import { ApiResponse } from '../models/response.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private oauthService = inject(OAuthService);

  private authStateSubject = new BehaviorSubject<AuthState>({
    isAuthenticated: false,
    loading: true
  });

  public authState$ = this.authStateSubject.asObservable();

  constructor() {
    this.configureOAuth();
    this.initAuth();
  }

  private configureOAuth(): void {
    this.oauthService.configure({
      issuer: environment.oidc.issuer,
      redirectUri: environment.oidc.redirectUri,
      clientId: environment.oidc.clientId,
      responseType: environment.oidc.responseType,
      scope: environment.oidc.scope,
      showDebugInformation: environment.oidc.showDebugInformation,
      requireHttps: environment.oidc.requireHttps,
      strictDiscoveryDocumentValidation: environment.oidc.strictDiscoveryDocumentValidation,
      postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
      useSilentRefresh: true,
      silentRefreshTimeout: 5000,
      timeoutFactor: 0.75,
    });

    this.oauthService.setupAutomaticSilentRefresh();

    // Listen to OAuth events
    this.oauthService.events.subscribe(event => {
      if (event instanceof OAuthErrorEvent) {
        console.error('OAuth error:', event);
      }
    });
  }

  private async initAuth(): Promise<void> {
    try {
      await this.oauthService.loadDiscoveryDocument();
      
      // Check if we have a valid token
      if (this.oauthService.hasValidAccessToken()) {
        await this.loadUserProfile();
      } else {
        // Try to get token from URL (OAuth callback)
        await this.oauthService.tryLogin();
        
        if (this.oauthService.hasValidAccessToken()) {
          await this.loadUserProfile();
        }
      }
    } catch (error) {
      console.error('Auth initialization error:', error);
    } finally {
      this.authStateSubject.next({
        ...this.authStateSubject.value,
        loading: false
      });
    }
  }

  // Cookie-based login (for simple scenarios)
  login(request: LoginRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(
      `${environment.gatewayUrl}/api/account/login`,
      request,
      { withCredentials: true }
    ).pipe(
      tap(response => {
        if (response.isSuccess) {
          // After cookie login, initiate OAuth flow for token
          this.startOAuthFlow();
        }
      })
    );
  }

  // OAuth PKCE flow (recommended for SPA)
  startOAuthFlow(): void {
    this.oauthService.initCodeFlow();
  }

  register(request: RegisterRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(
      `${environment.gatewayUrl}/api/account/register`,
      request
    );
  }

  sendOtp(request: SendOtpRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(
      `${environment.gatewayUrl}/api/account/send-otp`,
      request
    );
  }

  verifyOtp(request: VerifyOtpRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(
      `${environment.gatewayUrl}/api/account/verify-otp`,
      request
    );
  }

  async loadUserProfile(): Promise<void> {
    try {
      const response = await this.http.get<ApiResponse<UserProfile>>(
        `${environment.gatewayUrl}/api/account/profile`,
        { withCredentials: true }
      ).toPromise();

      if (response?.isSuccess && response.result) {
        this.authStateSubject.next({
          isAuthenticated: true,
          userId: response.result.id,
          profile: response.result,
          accessToken: this.oauthService.getAccessToken(),
          loading: false
        });
      }
    } catch (error) {
      console.error('Failed to load user profile:', error);
      this.authStateSubject.next({
        isAuthenticated: false,
        loading: false
      });
    }
  }

  logout(): void {
    this.oauthService.logOut();
    this.authStateSubject.next({
      isAuthenticated: false,
      loading: false
    });
    this.router.navigate(['/']);
  }

  get isAuthenticated(): boolean {
    return this.authStateSubject.value.isAuthenticated;
  }

  get currentUser(): UserProfile | undefined {
    return this.authStateSubject.value.profile;
  }

  get accessToken(): string | null {
    return this.oauthService.getAccessToken();
  }

  hasRole(role: string): boolean {
    return this.currentUser?.roles?.includes(role) ?? false;
  }

  isAdmin(): boolean {
    return this.hasRole('Admin');
  }
}