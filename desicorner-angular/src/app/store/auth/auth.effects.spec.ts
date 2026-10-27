import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { Action } from '@ngrx/store';
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable, of } from 'rxjs';
import { environment } from '@env/environment';
import { RegisterRequest, SendOtpRequest, UserProfile, VerifyOtpRequest } from '../../core/models/auth.models';
import { AuthActions } from './auth.actions';
import { AuthEffects } from './auth.effects';

describe('AuthEffects', () => {
  const api = `${environment.gatewayUrl}/api/account`;
  const profile = { id: 'u1', email: 'a@example.test', roles: ['Customer'] } as unknown as UserProfile;

  let actions$: Observable<Action>;
  let effects: AuthEffects;
  let httpMock: HttpTestingController;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    TestBed.configureTestingModule({
      providers: [
        AuthEffects,
        provideMockActions(() => actions$),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    });
    effects = TestBed.inject(AuthEffects);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.removeItem('access_token');
    localStorage.removeItem('desicorner_user');
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.removeItem('access_token');
    localStorage.removeItem('desicorner_user');
  });

  /** Collects everything an effect emits for the given input actions. */
  function run(effect: Observable<Action>): Action[] {
    const out: Action[] = [];
    effect.subscribe(a => out.push(a));
    return out;
  }

  describe('checkAuth$', () => {
    it('reports success when an access token is stored', () => {
      localStorage.setItem('access_token', 'tok');
      actions$ = of(AuthActions.checkAuth());

      expect(run(effects.checkAuth$)).toEqual([AuthActions.checkAuthSuccess()]);
    });

    it('reports no token otherwise', () => {
      actions$ = of(AuthActions.checkAuth());

      expect(run(effects.checkAuth$)).toEqual([AuthActions.checkAuthNoToken()]);
    });
  });

  it('loads the profile after checkAuthSuccess and after a PKCE callback', () => {
    actions$ = of(AuthActions.checkAuthSuccess());
    expect(run(effects.checkAuthLoadProfile$)).toEqual([AuthActions.loadUserProfile()]);

    actions$ = of(AuthActions.pkceCallbackSuccess());
    expect(run(effects.pkceCallbackLoadProfile$)).toEqual([AuthActions.loadUserProfile()]);
  });

  describe('loadUserProfile$', () => {
    beforeEach(() => (actions$ = of(AuthActions.loadUserProfile())));

    it('GETs the profile, caches it and emits success', () => {
      const out = run(effects.loadUserProfile$);

      const req = httpMock.expectOne(`${api}/profile`);
      expect(req.request.method).toBe('GET');
      req.flush({ isSuccess: true, message: '', result: profile });

      expect(out).toEqual([AuthActions.loadUserProfileSuccess({ profile })]);
      expect(JSON.parse(localStorage.getItem('desicorner_user')!)).toEqual(profile as unknown as object);
    });

    it('emits failure when the API reports no profile', () => {
      const out = run(effects.loadUserProfile$);
      httpMock.expectOne(`${api}/profile`).flush({ isSuccess: false, message: 'nope' });

      expect(out).toEqual([AuthActions.loadUserProfileFailure({ error: 'Failed to load profile' })]);
      expect(localStorage.getItem('desicorner_user')).toBeNull();
    });

    it('emits failure with the server message on an HTTP error', () => {
      const out = run(effects.loadUserProfile$);
      httpMock.expectOne(`${api}/profile`).flush({ message: 'Token expired' }, { status: 401, statusText: 'Unauthorized' });

      expect(out).toEqual([AuthActions.loadUserProfileFailure({ error: 'Token expired' })]);
    });
  });

  it('logs out when the profile cannot be loaded', () => {
    actions$ = of(AuthActions.loadUserProfileFailure({ error: 'x' }));

    expect(run(effects.profileFailureLogout$)).toEqual([AuthActions.logout()]);
  });

  describe('logout$', () => {
    const tokenKeys = [
      'access_token', 'refresh_token', 'id_token', 'granted_scopes', 'access_token_stored_at',
      'id_token_stored_at', 'id_token_expires_at', 'id_token_claims_obj', 'nonce', 'PKCE_verifier',
      'session_state', 'token_expiry', 'desicorner_user', 'guest_session_id', 'desicorner_guest_session',
    ];

    afterEach(() => {
      for (const key of tokenKeys) localStorage.removeItem(key);
      localStorage.removeItem('desicorner_cart');
    });

    it('clears tokens, the cached user and the guest session, then navigates home', () => {
      for (const key of tokenKeys) localStorage.setItem(key, 'x');
      actions$ = of(AuthActions.logout());

      run(effects.logout$);

      for (const key of tokenKeys) {
        expect(localStorage.getItem(key)).withContext(key).toBeNull();
      }
      expect(router.navigate).toHaveBeenCalledOnceWith(['/']);
    });

    // BUG: logout resets the in-memory cart (cartReducer handles AuthActions.logout) but leaves
    // the persisted copy in localStorage. CartEffects falls back to that copy whenever the cart
    // API call fails or returns no cart, so the previous user's cart can reappear for the next
    // visitor on the same browser.
    xit('also clears the persisted cart', () => {
      localStorage.setItem('desicorner_cart', JSON.stringify({ items: [{ productId: 'p1' }] }));
      actions$ = of(AuthActions.logout());

      run(effects.logout$);

      expect(localStorage.getItem('desicorner_cart')).toBeNull();
    });
  });

  const posts: { name: string; url: string; effect: () => Observable<Action>; action: Action; success: (r: never) => Action; failure: (e: string) => Action; fallback: string; body: object }[] = [
    {
      name: 'register$',
      url: `${api}/register`,
      effect: () => effects.register$,
      body: { email: 'a@example.test', password: 'P@ssw0rd!' },
      action: AuthActions.register({ request: { email: 'a@example.test', password: 'P@ssw0rd!' } as RegisterRequest }),
      success: response => AuthActions.registerSuccess({ response }),
      failure: error => AuthActions.registerFailure({ error }),
      fallback: 'Registration failed',
    },
    {
      name: 'sendOtp$',
      url: `${api}/send-otp`,
      effect: () => effects.sendOtp$,
      body: { email: 'a@example.test', purpose: 'Checkout', deliveryMethod: 'Email' },
      action: AuthActions.sendOtp({ request: { email: 'a@example.test', purpose: 'Checkout', deliveryMethod: 'Email' } as SendOtpRequest }),
      success: response => AuthActions.sendOtpSuccess({ response }),
      failure: error => AuthActions.sendOtpFailure({ error }),
      fallback: 'Failed to send OTP',
    },
    {
      name: 'verifyOtp$',
      url: `${api}/verify-otp`,
      effect: () => effects.verifyOtp$,
      body: { email: 'a@example.test', otp: '123456' },
      action: AuthActions.verifyOtp({ request: { email: 'a@example.test', otp: '123456' } as unknown as VerifyOtpRequest }),
      success: response => AuthActions.verifyOtpSuccess({ response }),
      failure: error => AuthActions.verifyOtpFailure({ error }),
      fallback: 'OTP verification failed',
    },
  ];

  for (const p of posts) {
    describe(p.name, () => {
      beforeEach(() => (actions$ = of(p.action)));

      it('POSTs the request and passes the API response through', () => {
        const response = { isSuccess: true, message: 'ok' };
        const out = run(p.effect());

        const req = httpMock.expectOne(p.url);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(p.body);
        req.flush(response);

        expect(out).toEqual([p.success(response as never)]);
      });

      it('maps an HTTP error to failure with the server message, or a default', () => {
        let out = run(p.effect());
        httpMock.expectOne(p.url).flush({ message: 'Server says no' }, { status: 400, statusText: 'Bad Request' });
        expect(out).toEqual([p.failure('Server says no')]);

        out = run(p.effect());
        httpMock.expectOne(p.url).flush(null, { status: 500, statusText: 'Error' });
        expect(out).toEqual([p.failure(p.fallback)]);
      });
    });
  }
});
