import { createReducer, on } from '@ngrx/store';
import { UserProfile } from '../../core/models/auth.models';
import { AuthActions } from './auth.actions';

export interface AuthState {
  isAuthenticated: boolean;
  userId?: string;
  profile?: UserProfile;
  loading: boolean;
  error: string | null;
}

export const initialAuthState: AuthState = {
  isAuthenticated: false,
  loading: false,
  error: null,
};

export const authReducer = createReducer(
  initialAuthState,

  // PKCE Callback Success (tokens received via Authorization Code + PKCE)
  on(AuthActions.pkceCallbackSuccess, (state) => ({
    ...state,
    isAuthenticated: true,
    loading: true, // still loading until profile arrives
    error: null,
  })),
  on(AuthActions.loginFailure, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    loading: false,
    error,
  })),

  // Load User Profile
  on(AuthActions.loadUserProfileSuccess, (state, { profile }) => ({
    ...state,
    isAuthenticated: true,
    userId: profile.id,
    profile,
    loading: false,
    error: null,
  })),
  on(AuthActions.loadUserProfileFailure, (state, { error }) => ({
    ...state,
    isAuthenticated: false,
    userId: undefined,
    profile: undefined,
    loading: false,
    error,
  })),

  // Logout
  on(AuthActions.logout, () => ({ ...initialAuthState })),

  // Check Auth (on app init)
  on(AuthActions.checkAuthSuccess, (state) => ({
    ...state,
    isAuthenticated: true,
    loading: true,
  })),
  on(AuthActions.checkAuthNoToken, (state) => ({
    ...state,
    isAuthenticated: false,
    loading: false,
  })),

  // Token refresh failure triggers logout
  on(AuthActions.refreshTokenFailure, () => ({ ...initialAuthState })),
);
