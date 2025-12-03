import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap, catchError, throwError, switchMap, delay, map } from 'rxjs';
import { environment } from '@env/environment';
import { GuestSessionService} from './guest-session.service';
import { 
  LoginRequest, 
  RegisterRequest, 
  UserProfile, 
  AuthState,
  SendOtpRequest,
  VerifyOtpRequest,
  TokenResponse
} from '../models/auth.models';
import { ApiResponse } from '../models/response.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private guestSessionService = inject(GuestSessionService);

  private authStateSubject = new BehaviorSubject<AuthState>({
    isAuthenticated: false,
    loading: false
  });

  public authState$ = this.authStateSubject.asObservable();
  
  private readonly TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly TOKEN_EXPIRY_KEY = 'token_expiry';
  private readonly USER_KEY = 'desicorner_user';

  constructor() {
    // Check if user is logged in on init
    this.checkAuth();
  }

  private checkAuth(): void {
    const token = this.getToken();
    
    if (token && !this.isTokenExpired()) {
      // Token exists and is valid, load profile
      this.authStateSubject.next({ isAuthenticated: true, loading: true });
      this.loadUserProfile();
    } else {
      this.authStateSubject.next({ isAuthenticated: false, loading: false });
    }
  }

  /**
   * Login using OpenIddict Password Grant
   * Returns ApiResponse for backward compatibility with existing login component
   */
  login(request: LoginRequest): Observable<ApiResponse<any>> {
    this.authStateSubject.next({ isAuthenticated: false, loading: true });

    // Build form data for OAuth2 password grant
    const body = new URLSearchParams();
    body.set('grant_type', 'password');
    body.set('username', request.email);
    body.set('password', request.password);
    body.set('client_id', 'desicorner-angular');
    body.set('client_secret', 'secret_for_testing_password_grant');
    body.set('scope', 'openid profile email phone offline_access desicorner.products.read desicorner.products.write desicorner.cart desicorner.orders.read desicorner.orders.write desicorner.payment desicorner.admin');

    return this.http.post<TokenResponse>(
      `${environment.gatewayUrl}/connect/token`,
      body.toString(),
      {
        headers: new HttpHeaders({
          'Content-Type': 'application/x-www-form-urlencoded'
        })
      }
    ).pipe(
      tap(tokenResponse => {
    console.log('✅ Token received from OpenIddict:', {
        hasAccessToken: !!tokenResponse.access_token,
        hasRefreshToken: !!tokenResponse.refresh_token,
        expiresIn: tokenResponse.expires_in
    });
    
    // Store tokens
    localStorage.setItem(this.TOKEN_KEY, tokenResponse.access_token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, tokenResponse.refresh_token);
    localStorage.setItem(this.TOKEN_EXPIRY_KEY, (Date.now() + tokenResponse.expires_in * 1000).toString());
    
    // Verify it was saved
    console.log('💾 Token saved to localStorage:', {
        saved: !!localStorage.getItem(this.TOKEN_KEY)
    });
}),
      // Add delay to ensure localStorage is written
      delay(100),
      // Load profile after token is stored
      switchMap(() => {
        console.log('✅ Loading user profile...');
        this.authStateSubject.next({ isAuthenticated: true, loading: true });
        return this.loadUserProfileObservable();
      }),
      // Convert to ApiResponse format for backward compatibility
      map(() => {
        const response: ApiResponse<any> = {
          isSuccess: true,
          message: 'Login successful'
        };
        return response;
      }),
      catchError(error => {
        console.error('❌ Login failed:', error);
        this.authStateSubject.next({ isAuthenticated: false, loading: false });
        
        // Convert OAuth error to ApiResponse format
        const errorResponse: ApiResponse<any> = {
          isSuccess: false,
          message: error.error?.error_description || 'Login failed. Please check your credentials.'
        };
        return throwError(() => errorResponse);
      })
    );
  }

  /**
   * Load user profile - returns Observable for chaining
   */
  private loadUserProfileObservable(): Observable<UserProfile> {
    return this.http.get<ApiResponse<UserProfile>>(
      `${environment.gatewayUrl}/api/account/profile`
    ).pipe(
      map(response => {
        if (response?.isSuccess && response.result) {
          return response.result;
        }
        throw new Error('Failed to load profile');
      }),
      tap(profile => {
        console.log('✅ Profile loaded:', profile.email);
        this.setStoredUser(profile);
        this.authStateSubject.next({
          isAuthenticated: true,
          userId: profile.id,
          profile: profile,
          loading: false
        });
      }),
      catchError(error => {
        console.error('❌ Failed to load profile:', error);
        this.logout();
        return throwError(() => error);
      })
    );
  }

  /**
   * Load user profile - void method for manual calls
   */
  loadUserProfile(): void {
    this.loadUserProfileObservable().subscribe({
      error: (error) => {
        console.error('Profile load error:', error);
      }
    });
  }

  /**
   * Register new user
   */
  register(request: RegisterRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(
      `${environment.gatewayUrl}/api/account/register`,
      request
    );
  }

  /**
   * Send OTP
   */
  sendOtp(request: SendOtpRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(
      `${environment.gatewayUrl}/api/account/send-otp`,
      request
    );
  }

  /**
   * Verify OTP
   */
  verifyOtp(request: VerifyOtpRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(
      `${environment.gatewayUrl}/api/account/verify-otp`,
      request
    );
  }

  /**
   * Refresh access token
   */
  refreshToken(): Observable<TokenResponse> {
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    const body = new URLSearchParams();
    body.set('grant_type', 'refresh_token');
    body.set('refresh_token', refreshToken);
    body.set('client_id', 'desicorner-angular');
    body.set('client_secret', 'secret_for_testing_password_grant');

    return this.http.post<TokenResponse>(
      `${environment.gatewayUrl}/connect/token`,
      body.toString(),
      {
        headers: new HttpHeaders({
          'Content-Type': 'application/x-www-form-urlencoded'
        })
      }
    ).pipe(
      tap(tokenResponse => {
        localStorage.setItem(this.TOKEN_KEY, tokenResponse.access_token);
        localStorage.setItem(this.REFRESH_TOKEN_KEY, tokenResponse.refresh_token);
        localStorage.setItem(this.TOKEN_EXPIRY_KEY, (Date.now() + tokenResponse.expires_in * 1000).toString());
      }),
      catchError(error => {
        console.error('Token refresh failed:', error);
        this.logout();
        return throwError(() => error);
      })
    );
  }

  /**
   * Logout
   */
  logout(): void {
  // Clear auth tokens
  localStorage.removeItem(this.TOKEN_KEY);
  localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  localStorage.removeItem(this.TOKEN_EXPIRY_KEY);
  this.removeStoredUser();
  
  // Clear guest session (so interceptor will create a new one)
  localStorage.removeItem('guest_session_id');
  this.guestSessionService.clearSession();
  
  // Update auth state
  this.authStateSubject.next({
    isAuthenticated: false,
    loading: false
  });
  
  // Navigate to home
  this.router.navigate(['/']);
}

  // Token management
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private isTokenExpired(): boolean {
    const expiry = localStorage.getItem(this.TOKEN_EXPIRY_KEY);
    if (!expiry) return true;
    return Date.now() >= parseInt(expiry);
  }

  // User management
  private getStoredUser(): UserProfile | null {
    const user = localStorage.getItem(this.USER_KEY);
    return user ? JSON.parse(user) : null;
  }

  private setStoredUser(user: UserProfile): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private removeStoredUser(): void {
    localStorage.removeItem(this.USER_KEY);
  }

  // Getters
  get isAuthenticated(): boolean {
    return this.authStateSubject.value.isAuthenticated;
  }

  get currentUser(): UserProfile | undefined {
    return this.authStateSubject.value.profile;
  }

  get accessToken(): string | null {
    return this.getToken();
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