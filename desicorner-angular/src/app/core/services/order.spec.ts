import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { CreateOrderRequest } from '../models/order.models';
import { OrderService } from './order.service';

describe('OrderService', () => {
  const ordersUrl = `${environment.gatewayUrl}/api/orders`;
  let service: OrderService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('createOrder POSTs the request to /api/orders and returns the API response', () => {
    const request: CreateOrderRequest = {
      orderType: 'Pickup',
      paymentMethod: 'Stripe',
      paymentIntentId: 'pi_123',
    } as CreateOrderRequest;
    const response = { isSuccess: true, message: 'Order placed successfully', result: { id: 'o1', orderNumber: 'DC-1' } };
    let received: unknown;

    service.createOrder(request).subscribe(r => (received = r));

    const req = httpMock.expectOne(ordersUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(response);
    expect(received).toEqual(response);
  });

  it('getOrderById GETs /api/orders/{id}', () => {
    service.getOrderById('o1').subscribe();

    const req = httpMock.expectOne(`${ordersUrl}/o1`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('getOrderByNumber GETs /api/orders/number/{orderNumber}', () => {
    service.getOrderByNumber('DC-2610271200-1234').subscribe();

    const req = httpMock.expectOne(`${ordersUrl}/number/DC-2610271200-1234`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('getMyOrders defaults to page 1 of 10', () => {
    service.getMyOrders().subscribe();

    const req = httpMock.expectOne(`${ordersUrl}/my-orders?page=1&pageSize=10`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('getMyOrders passes the requested page and page size', () => {
    service.getMyOrders(3, 25).subscribe();

    httpMock.expectOne(`${ordersUrl}/my-orders?page=3&pageSize=25`).flush({});
  });

  it('cancelOrder POSTs an empty body to /api/orders/{id}/cancel', () => {
    service.cancelOrder('o1').subscribe();

    const req = httpMock.expectOne(`${ordersUrl}/o1/cancel`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  it('maps order statuses to display text, passing unknown ones through', () => {
    expect(service.getStatusDisplay('Pending')).toBe('Order Placed');
    expect(service.getStatusDisplay('OutForDelivery')).toBe('Out for Delivery');
    expect(service.getStatusDisplay('Delivered')).toBe('Delivered');
    expect(service.getStatusDisplay('Refunded')).toBe('Refunded');
  });

  it('maps order statuses to CSS classes, with a default for unknown ones', () => {
    expect(service.getStatusColor('Pending')).toBe('status-pending');
    expect(service.getStatusColor('OutForDelivery')).toBe('status-delivery');
    expect(service.getStatusColor('Cancelled')).toBe('status-cancelled');
    expect(service.getStatusColor('Refunded')).toBe('status-default');
  });
});
