import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { OtpService } from './otp.service';

describe('OtpService', () => {
  const accountUrl = `${environment.gatewayUrl}/api/account`;
  let service: OtpService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(OtpService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sendOtp POSTs the email, purpose and Email delivery to /send-otp and returns the response', () => {
    let received: unknown;
    service.sendOtp('guest@example.test', 'Checkout').subscribe(r => (received = r));

    const req = httpMock.expectOne(`${accountUrl}/send-otp`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'guest@example.test', purpose: 'Checkout', deliveryMethod: 'Email' });
    req.flush({ isSuccess: true, message: 'OTP sent' });

    expect(received).toEqual({ isSuccess: true, message: 'OTP sent' });
  });

  it('sendOtp defaults the purpose to "Order Verification"', () => {
    service.sendOtp('guest@example.test').subscribe();

    expect(httpMock.expectOne(`${accountUrl}/send-otp`).request.body.purpose).toBe('Order Verification');
  });

  it('verifyOtp POSTs the email as the identifier with the code (VerifyOtpRequestDto shape)', () => {
    let received: unknown;
    service.verifyOtp('guest@example.test', '123456').subscribe(r => (received = r));

    const req = httpMock.expectOne(`${accountUrl}/verify-otp`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ identifier: 'guest@example.test', otp: '123456' });
    req.flush({ isSuccess: false, message: 'Invalid code' });

    expect(received).toEqual({ isSuccess: false, message: 'Invalid code' });
  });
});
