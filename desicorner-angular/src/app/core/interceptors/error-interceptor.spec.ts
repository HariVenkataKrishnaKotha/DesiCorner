import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { errorInterceptor } from './error-interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: jasmine.SpyObj<Router>;
  let toastr: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    toastr = jasmine.createSpyObj<ToastrService>('ToastrService', ['error']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: ToastrService, useValue: toastr },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  /** Sends a request, fails it with the given status and body, and returns the error the caller saw. */
  function fail(url: string, status: number, body: object | null = null): HttpErrorResponse {
    let received: HttpErrorResponse | undefined;
    http.get(url).subscribe({ error: e => (received = e) });
    httpMock.expectOne(url).flush(body, { status, statusText: 'Error' });
    return received!;
  }

  const cases: { status: number; body: object | null; message: string }[] = [
    { status: 400, body: { error: 'invalid_grant', error_description: 'The credentials are invalid.' }, message: 'The credentials are invalid.' },
    { status: 400, body: { message: 'Quantity must be positive' }, message: 'Quantity must be positive' },
    { status: 400, body: null, message: 'Bad request' },
    { status: 403, body: null, message: 'You do not have permission to perform this action' },
    { status: 404, body: null, message: 'Resource not found' },
    { status: 500, body: { message: 'ignored' }, message: 'Internal server error. Please try again later.' },
    { status: 502, body: { message: 'Bad gateway upstream' }, message: 'Bad gateway upstream' },
    { status: 503, body: null, message: 'Error: 503' },
  ];

  for (const c of cases) {
    it(`shows "${c.message}" for a ${c.status}${c.body ? ' with a body' : ''}`, () => {
      fail('/api/products', c.status, c.body);

      expect(toastr.error).toHaveBeenCalledOnceWith(c.message, 'Error');
      expect(router.navigate).not.toHaveBeenCalled();
    });
  }

  it('on 401 from a regular endpoint, reports an expired session and redirects to login', () => {
    fail('/api/orders/my-orders', 401);

    expect(toastr.error).toHaveBeenCalledOnceWith('Your session has expired. Please login again.', 'Error');
    expect(router.navigate).toHaveBeenCalledOnceWith(['/auth/login']);
  });

  for (const url of ['/api/account/check-auth', '/api/account/profile']) {
    it(`on 401 from ${url}, stays silent (the user is simply not logged in)`, () => {
      fail(url, 401);

      expect(toastr.error).not.toHaveBeenCalled();
      expect(router.navigate).not.toHaveBeenCalled();
    });
  }

  for (const url of ['/api/account/login', '/connect/token']) {
    it(`on 401 from ${url}, shows a login failure without redirecting`, () => {
      fail(url, 401);

      expect(toastr.error).toHaveBeenCalledOnceWith('Invalid email or password', 'Error');
      expect(router.navigate).not.toHaveBeenCalled();
    });
  }

  it('on 401 from login, prefers the server message', () => {
    fail('/api/account/login', 401, { message: 'Account locked' });

    expect(toastr.error).toHaveBeenCalledOnceWith('Account locked', 'Error');
  });

  it('reports client-side (network) errors from the ErrorEvent', () => {
    http.get('/api/cart').subscribe({ error: () => {} });
    httpMock.expectOne('/api/cart').error(new ErrorEvent('error', { message: 'Network down' }));

    expect(toastr.error).toHaveBeenCalledOnceWith('Error: Network down', 'Error');
  });

  it('rethrows the original HttpErrorResponse to the caller', () => {
    const error = fail('/api/products', 404, { message: 'nope' });

    expect(error).toEqual(jasmine.any(HttpErrorResponse));
    expect(error.status).toBe(404);
    expect(error.error).toEqual({ message: 'nope' });
  });

  it('lets successful responses through without a toast', () => {
    let body: unknown;
    http.get('/api/products').subscribe(b => (body = b));
    httpMock.expectOne('/api/products').flush({ isSuccess: true });

    expect(body).toEqual({ isSuccess: true });
    expect(toastr.error).not.toHaveBeenCalled();
  });
});
