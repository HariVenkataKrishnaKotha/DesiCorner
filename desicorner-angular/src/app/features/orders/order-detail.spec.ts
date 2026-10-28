import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ToastrService } from 'ngx-toastr';
import { environment } from '@env/environment';
import { AuthService } from '../../core/services/auth.service';
import { OrderDetailComponent } from './order-detail';

describe('OrderDetailComponent', () => {
  const orderUrl = `${environment.gatewayUrl}/api/orders/o1`;
  let httpMock: HttpTestingController;

  function setUp(isAuthenticated: boolean): ComponentFixture<OrderDetailComponent> {
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'o1' }), queryParamMap: convertToParamMap({}) } } },
        { provide: ToastrService, useValue: jasmine.createSpyObj('ToastrService', ['success', 'error']) },
        { provide: AuthService, useValue: { isAuthenticated } },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(OrderDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ComponentFixture<OrderDetailComponent>): string {
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  afterEach(() => httpMock.verify());

  it('shows the order when it loads', () => {
    const fixture = setUp(true);

    httpMock.expectOne(orderUrl).flush({
      isSuccess: true,
      message: '',
      result: {
        id: 'o1', orderNumber: 'DC-2610281200-1234', status: 'Confirmed', orderType: 'Pickup',
        items: [], subTotal: 12.5, taxAmount: 0, deliveryFee: 0, discountAmount: 0, total: 12.5,
        orderDate: new Date().toISOString(), createdAt: new Date().toISOString(),
        paymentMethod: 'Stripe', paymentStatus: 'Paid', userEmail: 'guest@example.test', userPhone: '5550100',
      },
    });

    expect(text(fixture)).toContain('DC-2610281200-1234');
  });

  describe('when the order is not found (404)', () => {
    function notFound(isAuthenticated: boolean): string {
      const fixture = setUp(isAuthenticated);
      httpMock.expectOne(orderUrl).flush({ isSuccess: false, message: 'Order not found' }, { status: 404, statusText: 'Not Found' });
      return text(fixture);
    }

    it('shows a friendly not-found message instead of an error', () => {
      const page = notFound(false);

      expect(page).toContain('Order not found');
      expect(page).toContain("We couldn't find this order");
      expect(page).not.toContain('Error Loading Order');
    });

    it('offers guests a way back to the menu, not to the sign-in-only order list', () => {
      const page = notFound(false);

      expect(page).toContain('Continue Shopping');
      expect(page).not.toContain('View All Orders');
    });

    it('offers signed-in users their order list', () => {
      expect(notFound(true)).toContain('View All Orders');
    });
  });

  it('keeps the error card for other failures', () => {
    const fixture = setUp(true);

    httpMock.expectOne(orderUrl).flush({ message: 'Something broke' }, { status: 500, statusText: 'Error' });

    const page = text(fixture);
    expect(page).toContain('Error Loading Order');
    expect(page).toContain('Something broke');
  });
});
