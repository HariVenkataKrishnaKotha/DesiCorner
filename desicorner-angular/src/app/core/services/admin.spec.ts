import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { CreateCouponRequest, UpdateCouponRequest } from '../models/admin.models';
import { CreateProductRequest, Product } from '../models/product.models';
import { AdminService } from './admin.service';

describe('AdminService', () => {
  const api = environment.gatewayUrl;
  let service: AdminService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function expectRequest(method: string, url: string): TestRequest {
    const req = httpMock.expectOne(r => r.url === url);
    expect(req.request.method).withContext(url).toBe(method);
    return req;
  }

  function form(req: TestRequest): FormData {
    expect(req.request.body).toEqual(jasmine.any(FormData));
    return req.request.body as FormData;
  }

  const image = new File(['png-bytes'], 'samosa.png', { type: 'image/png' });

  describe('products', () => {
    const product: CreateProductRequest = {
      name: 'Samosa', description: 'Crispy', price: 4.5, categoryId: 'c1', isAvailable: true,
      isVegetarian: true, isVegan: false, isSpicy: true, spiceLevel: 2, preparationTime: 10, allergens: 'gluten',
    };

    it('createProduct POSTs multipart form data with every field and the image', () => {
      let received: unknown;
      service.createProduct(product, image).subscribe(r => (received = r));

      const req = expectRequest('POST', `${api}/api/products`);
      const body = form(req);
      expect(body.get('name')).toBe('Samosa');
      expect(body.get('description')).toBe('Crispy');
      expect(body.get('price')).toBe('4.5');
      expect(body.get('categoryId')).toBe('c1');
      expect(body.get('isAvailable')).toBe('true');
      expect(body.get('isVegetarian')).toBe('true');
      expect(body.get('isVegan')).toBe('false');
      expect(body.get('isSpicy')).toBe('true');
      expect(body.get('spiceLevel')).toBe('2');
      expect(body.get('preparationTime')).toBe('10');
      expect(body.get('allergens')).toBe('gluten');
      expect((body.get('image') as File).name).toBe('samosa.png');
      req.flush({ isSuccess: true, message: 'Created', result: { id: 'p1' } });

      expect(received).toEqual({ isSuccess: true, message: 'Created', result: { id: 'p1' } });
    });

    it('createProduct omits allergens and image when not given', () => {
      service.createProduct({ ...product, allergens: undefined }).subscribe();

      const body = form(expectRequest('POST', `${api}/api/products`));
      expect(body.has('allergens')).toBeFalse();
      expect(body.has('image')).toBeFalse();
    });

    it('updateProduct PUTs multipart form data, including the id', () => {
      service.updateProduct('p1', { ...product, id: 'p1' } as Partial<Product>).subscribe();

      const req = expectRequest('PUT', `${api}/api/products/p1`);
      const body = form(req);
      expect(body.get('id')).toBe('p1');
      expect(body.get('name')).toBe('Samosa');
      expect(body.get('price')).toBe('4.5');
      expect(body.has('image')).toBeFalse();
      req.flush({ isSuccess: true });
    });

    it('deleteProduct, updateProductImage and deleteProductImage hit the product routes', () => {
      service.deleteProduct('p1').subscribe();
      expectRequest('DELETE', `${api}/api/products/p1`).flush({ isSuccess: true });

      service.updateProductImage('p1', image).subscribe();
      const upload = expectRequest('POST', `${api}/api/products/p1/image`);
      expect((form(upload).get('image') as File).name).toBe('samosa.png');
      upload.flush({ isSuccess: true });

      service.deleteProductImage('p1').subscribe();
      expectRequest('DELETE', `${api}/api/products/p1/image`).flush({ isSuccess: true });
    });

    it('getProductStats GETs /api/products/admin/stats', () => {
      service.getProductStats().subscribe();
      expectRequest('GET', `${api}/api/products/admin/stats`).flush({ isSuccess: true });
    });
  });

  describe('categories', () => {
    it('createCategory POSTs multipart form data, with the image only when given', () => {
      service.createCategory('Snacks', 'Small bites', 2, image).subscribe();
      let body = form(expectRequest('POST', `${api}/api/categories`));
      expect(body.get('name')).toBe('Snacks');
      expect(body.get('description')).toBe('Small bites');
      expect(body.get('displayOrder')).toBe('2');
      expect(body.has('image')).toBeTrue();
      httpMock.match(() => true).forEach(r => r.flush({ isSuccess: true }));

      service.createCategory('Drinks', '', 3).subscribe();
      body = form(expectRequest('POST', `${api}/api/categories`));
      expect(body.has('image')).toBeFalse();
      httpMock.match(() => true).forEach(r => r.flush({ isSuccess: true }));
    });

    it('updateCategory PUTs multipart form data including the id', () => {
      service.updateCategory('c1', 'Snacks', 'Small bites', 1).subscribe();

      const req = expectRequest('PUT', `${api}/api/categories/c1`);
      expect(form(req).get('id')).toBe('c1');
      expect(form(req).get('displayOrder')).toBe('1');
      req.flush({ isSuccess: true });
    });

    it('deleteCategory, updateCategoryImage and deleteCategoryImage hit the category routes', () => {
      service.deleteCategory('c1').subscribe();
      expectRequest('DELETE', `${api}/api/categories/c1`).flush({ isSuccess: true });

      service.updateCategoryImage('c1', image).subscribe();
      expectRequest('POST', `${api}/api/categories/c1/image`).flush({ isSuccess: true });

      service.deleteCategoryImage('c1').subscribe();
      expectRequest('DELETE', `${api}/api/categories/c1/image`).flush({ isSuccess: true });
    });
  });

  describe('orders', () => {
    it('getAllOrders GETs /api/orders/admin/all with every filter as a query param (empty when unset)', () => {
      service.getAllOrders({ page: 2, pageSize: 20, sortBy: 'total', sortDescending: true, status: 'Pending' }).subscribe();

      const req = expectRequest('GET', `${api}/api/orders/admin/all`);
      const p = req.request.params;
      expect(p.get('page')).toBe('2');
      expect(p.get('pageSize')).toBe('20');
      expect(p.get('sortBy')).toBe('total');
      expect(p.get('sortDescending')).toBe('true');
      expect(p.get('status')).toBe('Pending');
      expect(p.get('paymentStatus')).toBe('');
      expect(p.get('searchTerm')).toBe('');
      expect(p.get('fromDate')).toBe('');
      expect(p.get('toDate')).toBe('');
      req.flush({ isSuccess: true, result: { orders: [], totalCount: 0 } });
    });

    it('getOrderStats and getRecentOrders hit the admin order routes', () => {
      service.getOrderStats().subscribe();
      expectRequest('GET', `${api}/api/orders/admin/stats`).flush({ isSuccess: true });

      service.getRecentOrders().subscribe();
      httpMock.expectOne(`${api}/api/orders/admin/recent?count=5`).flush({ isSuccess: true });

      service.getRecentOrders(12).subscribe();
      httpMock.expectOne(`${api}/api/orders/admin/recent?count=12`).flush({ isSuccess: true });
    });

    it('updateOrderStatus PUTs { orderId, status, notes } to /api/orders/status (UpdateOrderStatusDto)', () => {
      service.updateOrderStatus('o1', 'Preparing', 'Started').subscribe();

      const req = expectRequest('PUT', `${api}/api/orders/status`);
      expect(req.request.body).toEqual({ orderId: 'o1', status: 'Preparing', notes: 'Started' });
      req.flush({ isSuccess: true });
    });

    it('getOrderById GETs /api/orders/{id}', () => {
      service.getOrderById('o1').subscribe();
      expectRequest('GET', `${api}/api/orders/o1`).flush({ isSuccess: true });
    });
  });

  describe('users', () => {
    it('getAllUsers GETs /api/admin/users with the filter as query params', () => {
      service.getAllUsers({ page: 1, pageSize: 25, sortBy: 'email', sortDescending: false, role: 'Admin', isLocked: false }).subscribe();

      const p = expectRequest('GET', `${api}/api/admin/users`).request.params;
      expect(p.get('page')).toBe('1');
      expect(p.get('pageSize')).toBe('25');
      expect(p.get('sortBy')).toBe('email');
      expect(p.get('sortDescending')).toBe('false');
      expect(p.get('role')).toBe('Admin');
      expect(p.get('isLocked')).toBe('false');
      expect(p.get('emailConfirmed')).toBe('');
      expect(p.get('searchTerm')).toBe('');
      httpMock.match(() => true).forEach(r => r.flush({ isSuccess: true }));
    });

    it('getUserStats, getRecentUsers and getAllRoles hit the admin routes', () => {
      service.getUserStats().subscribe();
      expectRequest('GET', `${api}/api/admin/users/stats`).flush({ isSuccess: true });

      service.getRecentUsers(3).subscribe();
      httpMock.expectOne(`${api}/api/admin/users/recent?count=3`).flush({ isSuccess: true });

      service.getAllRoles().subscribe();
      expectRequest('GET', `${api}/api/admin/roles`).flush({ isSuccess: true });
    });

    it('updateUserRole and toggleUserLock POST their bodies', () => {
      service.updateUserRole('u1', 'Admin', true).subscribe();
      let req = expectRequest('POST', `${api}/api/admin/users/role`);
      expect(req.request.body).toEqual({ userId: 'u1', role: 'Admin', add: true });
      req.flush({ isSuccess: true });

      service.toggleUserLock('u1', true, 'Fraud').subscribe();
      req = expectRequest('POST', `${api}/api/admin/users/lock`);
      expect(req.request.body).toEqual({ userId: 'u1', lock: true, reason: 'Fraud' });
      req.flush({ isSuccess: true });
    });
  });

  describe('coupons', () => {
    const coupon: CreateCouponRequest = {
      code: 'WELCOME10', discountAmount: 10, discountType: 'Percentage', minAmount: 20, isActive: true, maxUsageCount: 100,
    };

    it('getAllCoupons GETs /api/coupons with the filter as query params', () => {
      service.getAllCoupons({ page: 1, pageSize: 10, isActive: true }).subscribe();

      const p = expectRequest('GET', `${api}/api/coupons`).request.params;
      expect(p.get('page')).toBe('1');
      expect(p.get('pageSize')).toBe('10');
      expect(p.get('isActive')).toBe('true');
      expect(p.get('isExpired')).toBe('');
      expect(p.get('searchTerm')).toBe('');
      httpMock.match(() => true).forEach(r => r.flush({ isSuccess: true, result: [] }));
    });

    it('getCouponStats, createCoupon, updateCoupon and deleteCoupon hit the coupon routes', () => {
      service.getCouponStats().subscribe();
      expectRequest('GET', `${api}/api/coupons/stats`).flush({ isSuccess: true });

      service.createCoupon(coupon).subscribe();
      let req = expectRequest('POST', `${api}/api/coupons`);
      expect(req.request.body).toEqual(coupon);
      req.flush({ isSuccess: true });

      const update: UpdateCouponRequest = { ...coupon, id: 'k1', discountAmount: 15 };
      service.updateCoupon(update).subscribe();
      req = expectRequest('PUT', `${api}/api/coupons/k1`);
      expect(req.request.body).toEqual(update);
      req.flush({ isSuccess: true });

      service.deleteCoupon('k1').subscribe();
      expectRequest('DELETE', `${api}/api/coupons/k1`).flush({ isSuccess: true });
    });

    // CouponsController declares [HttpPost("{id}/toggle")]; any other verb gets 405.
    it('toggleCouponStatus POSTs to /api/coupons/{id}/toggle (the verb the API accepts)', () => {
      service.toggleCouponStatus('k1').subscribe();

      expectRequest('POST', `${api}/api/coupons/k1/toggle`).flush({ isSuccess: true });
    });
  });
});
