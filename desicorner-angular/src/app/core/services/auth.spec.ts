import { TestBed } from '@angular/core/testing';
import { Action } from '@ngrx/store';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { OAuthService } from 'angular-oauth2-oidc';
import { Subject } from 'rxjs';
import { environment } from '@env/environment';
import { AuthActions } from '../../store/auth/auth.actions';
import { initialAuthState } from '../../store/auth/auth.reducer';
import { initialCartState } from '../../store/cart/cart.reducer';
import { initialProductState } from '../../store/product/product.reducer';
import { ApiResponse } from '../models/response.models';
import { RegisterRequest, SendOtpRequest, UserProfile, VerifyOtpRequest } from '../models/auth.models';
import { AuthService } from './auth.service';

const SESSION_KEY = 'desicorner_guest_session';

describe('AuthService', () => {
  let actions$: Subject<Action>;
  let store: MockStore;
  let oauth: jasmine.SpyObj<OAuthService>;

  function profile(roles: string[]): UserProfile {
    return { id: 'u1', email: 'a@example.test', roles } as unknown as UserProfile;
  }

  function create(options: { tryLogin?: Promise<boolean>; validToken?: boolean } = {}): AuthService {
    oauth.loadDiscoveryDocumentAndTryLogin.and.returnValue(options.tryLogin ?? Promise.resolve(false));
    oauth.hasValidAccessToken.and.returnValue(options.validToken ?? false);
    return TestBed.inject(AuthService);
  }

  beforeEach(() => {
    actions$ = new Subject<Action>();
    oauth = jasmine.createSpyObj<OAuthService>('OAuthService', [
      'configure',
      'setupAutomaticSilentRefresh',
      'loadDiscoveryDocumentAndTryLogin',
      'hasValidAccessToken',
      'getAccessToken',
      'initCodeFlow',
    ]);
    localStorage.removeItem('access_token');
    localStorage.removeItem(SESSION_KEY);

    TestBed.configureTestingModule({
      providers: [
        provideMockStore({
          initialState: { auth: initialAuthState, cart: initialCartState, product: initialProductState },
        }),
        provideMockActions(() => actions$),
        { provide: OAuthService, useValue: oauth },
      ],
    });
    store = TestBed.inject(MockStore);
    spyOn(store, 'dispatch').and.callThrough();
  });

  afterEach(() => {
    localStorage.removeItem('access_token');
    localStorage.removeItem(SESSION_KEY);
  });

  describe('on construction', () => {
    it('configures OAuth for code + PKCE from the environment and starts silent refresh', () => {
      create();

      expect(oauth.configure).toHaveBeenCalledOnceWith(jasmine.objectContaining({
        issuer: environment.oidc.issuer,
        clientId: environment.oidc.clientId,
        redirectUri: environment.oidc.redirectUri,
        responseType: 'code',
        scope: environment.oidc.scope,
      }));
      expect(oauth.setupAutomaticSilentRefresh).toHaveBeenCalled();
    });

    it('dispatches checkAuth', () => {
      create();

      expect(store.dispatch).toHaveBeenCalledWith(AuthActions.checkAuth());
    });

    it('dispatches pkceCallbackSuccess when the discovery/login round trip yields a valid token', async () => {
      const tryLogin = Promise.resolve(true);
      create({ tryLogin, validToken: true });
      await tryLogin;

      expect(store.dispatch).toHaveBeenCalledWith(AuthActions.pkceCallbackSuccess());
    });

    it('does not dispatch pkceCallbackSuccess when not logged in', async () => {
      const tryLogin = Promise.resolve(false);
      create({ tryLogin });
      await tryLogin;

      expect(store.dispatch).not.toHaveBeenCalledWith(AuthActions.pkceCallbackSuccess());
    });
  });

  it('initLogin starts the authorization code flow', () => {
    create().initLogin();

    expect(oauth.initCodeFlow).toHaveBeenCalled();
  });

  describe('handlePkceCallback', () => {
    it('returns true and dispatches signInCompleted (a fresh sign-in) when tokens were received', async () => {
      const service = create();
      oauth.loadDiscoveryDocumentAndTryLogin.and.resolveTo(true);
      oauth.hasValidAccessToken.and.returnValue(true);
      (store.dispatch as jasmine.Spy).calls.reset();

      expect(await service.handlePkceCallback()).toBeTrue();
      expect(store.dispatch).toHaveBeenCalledOnceWith(AuthActions.signInCompleted());
    });

    it('returns false when there is no valid token', async () => {
      const service = create();
      oauth.loadDiscoveryDocumentAndTryLogin.and.resolveTo(true);
      oauth.hasValidAccessToken.and.returnValue(false);

      expect(await service.handlePkceCallback()).toBeFalse();
    });

    it('returns false when the exchange throws', async () => {
      const service = create();
      oauth.loadDiscoveryDocumentAndTryLogin.and.rejectWith(new Error('invalid_grant'));

      expect(await service.handlePkceCallback()).toBeFalse();
    });
  });

  it('loadUserProfile dispatches loadUserProfile', () => {
    create().loadUserProfile();

    expect(store.dispatch).toHaveBeenCalledWith(AuthActions.loadUserProfile());
  });

  describe('request/response facades', () => {
    const okResponse: ApiResponse = { isSuccess: true, message: 'ok' };

    it('register dispatches register and returns the success response', () => {
      const service = create();
      const request = { email: 'a@example.test' } as RegisterRequest;
      let result: ApiResponse | undefined;

      service.register(request).subscribe(r => (result = r));
      expect(store.dispatch).toHaveBeenCalledWith(AuthActions.register({ request }));
      actions$.next(AuthActions.registerSuccess({ response: okResponse }));

      expect(result).toEqual(okResponse);
    });

    it('register maps a failure to an unsuccessful response with the error message', () => {
      const service = create();
      let result: ApiResponse | undefined;

      service.register({} as RegisterRequest).subscribe(r => (result = r));
      actions$.next(AuthActions.registerFailure({ error: 'Email already registered' }));

      expect(result).toEqual({ isSuccess: false, message: 'Email already registered' });
    });

    it('sendOtp dispatches sendOtp and maps success and failure', () => {
      const service = create();
      const request = { email: 'a@example.test', purpose: 'Checkout', deliveryMethod: 'Email' } as SendOtpRequest;
      const results: ApiResponse[] = [];

      service.sendOtp(request).subscribe(r => results.push(r));
      expect(store.dispatch).toHaveBeenCalledWith(AuthActions.sendOtp({ request }));
      actions$.next(AuthActions.sendOtpSuccess({ response: okResponse }));
      service.sendOtp(request).subscribe(r => results.push(r));
      actions$.next(AuthActions.sendOtpFailure({ error: 'Too many requests' }));

      expect(results).toEqual([okResponse, { isSuccess: false, message: 'Too many requests' }]);
    });

    it('verifyOtp dispatches verifyOtp and maps success and failure', () => {
      const service = create();
      const request = { email: 'a@example.test', otp: '123456' } as unknown as VerifyOtpRequest;
      const results: ApiResponse[] = [];

      service.verifyOtp(request).subscribe(r => results.push(r));
      expect(store.dispatch).toHaveBeenCalledWith(AuthActions.verifyOtp({ request }));
      actions$.next(AuthActions.verifyOtpSuccess({ response: okResponse }));
      service.verifyOtp(request).subscribe(r => results.push(r));
      actions$.next(AuthActions.verifyOtpFailure({ error: 'Invalid code' }));

      expect(results).toEqual([okResponse, { isSuccess: false, message: 'Invalid code' }]);
    });

    it('takes only the first matching result', () => {
      const service = create();
      const results: ApiResponse[] = [];

      service.register({} as RegisterRequest).subscribe(r => results.push(r));
      actions$.next(AuthActions.registerSuccess({ response: okResponse }));
      actions$.next(AuthActions.registerFailure({ error: 'late' }));

      expect(results.length).toBe(1);
    });
  });

  describe('state getters', () => {
    it('isAuthenticated is true with a valid OAuth token even if the store says otherwise', () => {
      const service = create({ validToken: true });

      expect(service.isAuthenticated).toBeTrue();
    });

    it('isAuthenticated falls back to the store', () => {
      const service = create({ validToken: false });
      expect(service.isAuthenticated).toBeFalse();

      store.setState({ auth: { ...initialAuthState, isAuthenticated: true }, cart: initialCartState, product: initialProductState });
      expect(service.isAuthenticated).toBeTrue();
    });

    it('currentUser, hasRole and isAdmin read the profile from the store', () => {
      const service = create();
      expect(service.currentUser).toBeUndefined();
      expect(service.isAdmin()).toBeFalse();

      store.setState({
        auth: { ...initialAuthState, isAuthenticated: true, profile: profile(['Customer', 'Admin']) },
        cart: initialCartState,
        product: initialProductState,
      });

      expect(service.currentUser?.id).toBe('u1');
      expect(service.hasRole('Customer')).toBeTrue();
      expect(service.hasRole('Staff')).toBeFalse();
      expect(service.isAdmin()).toBeTrue();
    });

    it('accessToken prefers the OAuth library token, then localStorage', () => {
      const service = create();
      oauth.getAccessToken.and.returnValue('from-oauth');
      expect(service.accessToken).toBe('from-oauth');
      expect(service.getToken()).toBe('from-oauth');

      oauth.getAccessToken.and.returnValue('');
      localStorage.setItem('access_token', 'from-storage');
      expect(service.accessToken).toBe('from-storage');

      localStorage.removeItem('access_token');
      expect(service.accessToken).toBeNull();
    });

    it('isGuest is true only for an unauthenticated visitor with a guest session', () => {
      const service = create();
      expect(service.isGuest).toBeFalse();

      const sessionId = service.guestSessionId;
      expect(localStorage.getItem(SESSION_KEY)).toBe(sessionId);
      expect(service.isGuest).toBeTrue();

      service.clearGuestSession();
      expect(service.isGuest).toBeFalse();
    });
  });
});
