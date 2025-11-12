import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
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

interface LoginResponse {
  token: string;
  user: UserProfile;
  expiresIn: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private authStateSubject = new BehaviorSubject<AuthState>({
    isAuthenticated: false,
    loading: false
  });

  public authState$ = this.authStateSubject.asObservable();
  private readonly TOKEN_KEY = 'desicorner_token';
  private readonly USER_KEY = 'desicorner_user';

  constructor() {
    // Check if user is logged in on init
    this.checkAuth();
  }

  private checkAuth(): void {
    const token = this.getToken();
    const user = this.getStoredUser();

    if (token && user) {
      // Token exists, verify it's still valid
      this.loadUserProfile();
    } else {
      this.authStateSubject.next({ isAuthenticated: false, loading: false });
    }
  }

  login(request: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(
      `${environment.gatewayUrl}/api/account/login`,
      request
    ).pipe(
      tap(response => {
        if (response.isSuccess && response.result) {
          // Store token and user data
          this.setToken(response.result.token);
          this.setStoredUser(response.result.user);
          
          // Update auth state
          this.authStateSubject.next({
            isAuthenticated: true,
            userId: response.result.user.id,
            profile: response.result.user,
            loading: false
          });
        }
      })
    );
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

  loadUserProfile(): void {
    this.http.get<ApiResponse<UserProfile>>(
      `${environment.gatewayUrl}/api/account/profile`
    ).subscribe({
      next: (response) => {
        if (response?.isSuccess && response.result) {
          this.setStoredUser(response.result);
          this.authStateSubject.next({
            isAuthenticated: true,
            userId: response.result.id,
            profile: response.result,
            loading: false
          });
          console.log('User profile loaded:', response.result);
        } else {
          this.logout();
        }
      },
      error: (error) => {
        console.error('Failed to load profile:', error);
        this.logout();
      }
    });
  }

  logout(): void {
    // Clear local storage
    this.removeToken();
    this.removeStoredUser();
    
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

  private setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  private removeToken(): void {
    localStorage.removeItem(this.TOKEN_KEY);
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
}