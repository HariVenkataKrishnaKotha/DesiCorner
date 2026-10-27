import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { ApiService } from './api.service';

describe('ApiService', () => {
  const base = environment.gatewayUrl;
  let service: ApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs the endpoint on the gateway and returns the body', () => {
    let body: unknown;
    service.get('/api/products').subscribe(b => (body = b));

    const req = httpMock.expectOne(`${base}/api/products`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush({ isSuccess: true, result: [] });

    expect(body).toEqual({ isSuccess: true, result: [] });
  });

  it('turns params into query string values, skipping null and undefined', () => {
    service.get('/api/products', { page: 2, search: 'samosa', veg: false, category: null, sort: undefined }).subscribe();

    const req = httpMock.expectOne(r => r.url === `${base}/api/products`);
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('search')).toBe('samosa');
    expect(req.request.params.get('veg')).toBe('false');
    expect(req.request.params.has('category')).toBeFalse();
    expect(req.request.params.has('sort')).toBeFalse();
    req.flush({});
  });

  for (const method of ['post', 'put', 'patch'] as const) {
    it(`${method.toUpperCase()}s the body to the endpoint on the gateway`, () => {
      const payload = { name: 'Samosa', price: 4.5 };
      service[method]('/api/products/1', payload).subscribe();

      const req = httpMock.expectOne(`${base}/api/products/1`);
      expect(req.request.method).toBe(method.toUpperCase());
      expect(req.request.body).toEqual(payload);
      req.flush({});
    });
  }

  it('DELETEs the endpoint on the gateway', () => {
    service.delete('/api/products/1').subscribe();

    const req = httpMock.expectOne(`${base}/api/products/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });
});
