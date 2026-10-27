import { UserProfile } from '../../core/models/auth.models';
import { AuthActions } from './auth.actions';
import { AuthState, authReducer, initialAuthState } from './auth.reducer';
import {
  selectAuthStateForGuards,
  selectIsAdmin,
  selectUserId,
  selectUserRoles,
} from './auth.selectors';

describe('authReducer', () => {
  const profile = { id: 'u1', email: 'a@example.test', roles: ['Customer'] } as unknown as UserProfile;
  const signedIn: AuthState = { isAuthenticated: true, userId: 'u1', profile, loading: false, error: null };

  it('returns the initial state for an unknown action', () => {
    expect(authReducer(undefined, { type: 'unknown' })).toEqual(initialAuthState);
  });

  it('pkceCallbackSuccess marks the user authenticated but still loading the profile', () => {
    const state = authReducer({ ...initialAuthState, error: 'old' }, AuthActions.pkceCallbackSuccess());

    expect(state).toEqual({ ...initialAuthState, isAuthenticated: true, loading: true, error: null });
  });

  it('checkAuthSuccess marks authenticated and loading; checkAuthNoToken marks anonymous and settled', () => {
    expect(authReducer(initialAuthState, AuthActions.checkAuthSuccess()))
      .toEqual({ ...initialAuthState, isAuthenticated: true, loading: true });
    expect(authReducer({ ...initialAuthState, loading: true }, AuthActions.checkAuthNoToken()))
      .toEqual({ ...initialAuthState, isAuthenticated: false, loading: false });
  });

  it('loadUserProfileSuccess stores the profile and user id and finishes loading', () => {
    const state = authReducer({ ...initialAuthState, loading: true }, AuthActions.loadUserProfileSuccess({ profile }));

    expect(state).toEqual(signedIn);
  });

  it('loadUserProfileFailure signs the user out and records the error', () => {
    const state = authReducer(signedIn, AuthActions.loadUserProfileFailure({ error: 'expired' }));

    expect(state.isAuthenticated).toBeFalse();
    expect(state.userId).toBeUndefined();
    expect(state.profile).toBeUndefined();
    expect(state.loading).toBeFalse();
    expect(state.error).toBe('expired');
  });

  it('loginFailure records the error and clears authentication', () => {
    const state = authReducer({ ...initialAuthState, loading: true }, AuthActions.loginFailure({ error: 'bad' }));

    expect(state).toEqual({ ...initialAuthState, error: 'bad' });
  });

  for (const action of [AuthActions.logout(), AuthActions.refreshTokenFailure({ error: 'x' })]) {
    it(`${action.type} resets to the initial state`, () => {
      expect(authReducer(signedIn, action)).toEqual(initialAuthState);
    });
  }

  it('leaves state untouched for actions handled only by effects', () => {
    const before = { ...signedIn };
    expect(authReducer(signedIn, AuthActions.register({ request: {} as never }))).toEqual(before);
  });

  describe('selectors', () => {
    const root = (auth: AuthState) => ({ auth });

    it('expose the user id, roles and admin flag', () => {
      expect(selectUserId(root(signedIn))).toBe('u1');
      expect(selectUserRoles(root(initialAuthState))).toEqual([]);
      expect(selectIsAdmin(root(signedIn))).toBeFalse();
      expect(selectIsAdmin(root({ ...signedIn, profile: { ...profile, roles: ['Admin'] } }))).toBeTrue();
    });

    it('shape the state the guards consume', () => {
      expect(selectAuthStateForGuards(root(signedIn))).toEqual({ isAuthenticated: true, loading: false, profile });
    });
  });
});
