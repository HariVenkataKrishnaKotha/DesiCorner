import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from './auth-interceptor';

const TOKEN_KEY = 'access_token';
const SESSION_KEY = 'desicorner_guest_session';
const UUID_V4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(SESSION_KEY);
    spyOn(console, 'log');
    spyOn(console, 'warn');

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(SESSION_KEY);
  });

  it('attaches the stored access token as a Bearer header and no session id', () => {
    localStorage.setItem(TOKEN_KEY, 'abc.def.ghi');

    http.get('/api/cart').subscribe();

    const req = httpMock.expectOne('/api/cart');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
    expect(req.request.headers.has('X-Session-Id')).toBeFalse();
    req.flush({});
  });

  it('sends the existing guest session id when there is no token', () => {
    localStorage.setItem(SESSION_KEY, 'existing-session');

    http.get('/api/cart').subscribe();

    const req = httpMock.expectOne('/api/cart');
    expect(req.request.headers.get('X-Session-Id')).toBe('existing-session');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('creates and persists a UUID v4 guest session id when none exists, then reuses it', () => {
    http.get('/api/cart').subscribe();
    const first = httpMock.expectOne('/api/cart');
    const created = first.request.headers.get('X-Session-Id');
    first.flush({});

    expect(created).toMatch(UUID_V4);
    expect(localStorage.getItem(SESSION_KEY)).toBe(created);

    http.get('/api/products').subscribe();
    const second = httpMock.expectOne('/api/products');
    expect(second.request.headers.get('X-Session-Id')).toBe(created);
    second.flush({});
  });

  it('has no exclusion list: every request gets the credential, whatever its URL or method', () => {
    localStorage.setItem(TOKEN_KEY, 'tok');

    http.post('https://localhost:7001/connect/token', 'grant_type=refresh_token').subscribe();
    http.get('https://localhost:7001/.well-known/openid-configuration').subscribe();

    for (const req of httpMock.match(() => true)) {
      expect(req.request.headers.get('Authorization')).toBe('Bearer tok');
      req.flush({});
    }
  });

  it('passes the response through unchanged', () => {
    localStorage.setItem(TOKEN_KEY, 'tok');
    let body: unknown;

    http.get('/api/cart').subscribe(b => (body = b));
    httpMock.expectOne('/api/cart').flush({ isSuccess: true });

    expect(body).toEqual({ isSuccess: true });
  });
});
