import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AuthState } from './auth.reducer';

export const selectAuthState = createFeatureSelector<AuthState>('auth');

export const selectIsAuthenticated = createSelector(
  selectAuthState,
  (state) => state.isAuthenticated
);

export const selectAuthLoading = createSelector(
  selectAuthState,
  (state) => state.loading
);

export const selectUserProfile = createSelector(
  selectAuthState,
  (state) => state.profile
);

export const selectUserId = createSelector(
  selectAuthState,
  (state) => state.userId
);

export const selectAuthError = createSelector(
  selectAuthState,
  (state) => state.error
);

export const selectUserRoles = createSelector(
  selectUserProfile,
  (profile) => profile?.roles ?? []
);

export const selectIsAdmin = createSelector(
  selectUserRoles,
  (roles) => roles.includes('Admin')
);

// Composite selector matching the shape guards expect
export const selectAuthStateForGuards = createSelector(
  selectAuthState,
  (state) => ({
    isAuthenticated: state.isAuthenticated,
    loading: state.loading,
    profile: state.profile,
  })
);
