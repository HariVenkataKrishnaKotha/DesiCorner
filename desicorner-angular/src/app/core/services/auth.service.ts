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

  constructor() {
    // Check if user is logged in on init
    this.checkAuth();
  }

  private checkAuth(): void {
    this.authStateSubject.next({ ...this.authStateSubject.value, loading: true });
    
    // Check authentication status from backend
    this.http.get<ApiResponse<UserProfile>>(`${environment.gatewayUrl}/api/account/profile`, { withCredentials: true })
      .subscribe({
        next: (response) => {
          if (response.isSuccess && response.result) {
            this.authStateSubject.next({
              isAuthenticated: true,
              userId: response.result.id,
              profile: response.result,
              loading: false
            });
          } else {
            this.authStateSubject.next({ isAuthenticated: false, loading: false });
          }
        },
        error: () => {
          this.authStateSubject.next({ isAuthenticated: false, loading: false });
        }
      });
  }

  login(request: LoginRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(
      `${environment.gatewayUrl}/api/account/login`,
      request,
      { withCredentials: true }
    ).pipe(
      tap(response => {
        if (response.isSuccess) {
          // Immediately load profile after successful login
          setTimeout(() => this.loadUserProfile(), 100);
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
      `${environment.gatewayUrl}/api/account/profile`,
      { withCredentials: true }
    ).subscribe({
      next: (response) => {
        if (response?.isSuccess && response.result) {
          this.authStateSubject.next({
            isAuthenticated: true,
            userId: response.result.id,
            profile: response.result,
            loading: false
          });
          console.log('User profile loaded:', response.result);
        } else {
          this.authStateSubject.next({
            isAuthenticated: false,
            loading: false
          });
        }
      },
      error: (error) => {
        console.error('Failed to load profile:', error);
        this.authStateSubject.next({
          isAuthenticated: false,
          loading: false
        });
      }
    });
  }

  logout(): void {
    this.http.post(`${environment.gatewayUrl}/api/account/logout`, {}, { withCredentials: true })
      .subscribe({
        next: () => {
          this.authStateSubject.next({
            isAuthenticated: false,
            loading: false
          });
          this.router.navigate(['/']);
        },
        error: () => {
          this.authStateSubject.next({
            isAuthenticated: false,
            loading: false
          });
          this.router.navigate(['/']);
        }
      });
  }

  get isAuthenticated(): boolean {
    return this.authStateSubject.value.isAuthenticated;
  }

  get currentUser(): UserProfile | undefined {
    return this.authStateSubject.value.profile;
  }

  get accessToken(): string | null {
    return null;
  }

  hasRole(role: string): boolean {
    return this.currentUser?.roles?.includes(role) ?? false;
  }

  isAdmin(): boolean {
    return this.hasRole('Admin');
  }
}