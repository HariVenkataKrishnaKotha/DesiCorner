import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { AddAddressRequest, ChangePasswordRequest } from '../models/profile.models';
import { AuthService } from './auth.service';
import { ProfileService } from './profile.service';

describe('ProfileService', () => {
  const accountUrl = `${environment.gatewayUrl}/api/account`;
  let service: ProfileService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<AuthService>;

  const address: AddAddressRequest = {
    label: 'Home',
    addressLine1: '1 Main St',
    city: 'Austin',
    state: 'TX',
    zipCode: '73301',
    isDefault: true,
  };

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['loadUserProfile']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService },
      ],
    });
    service = TestBed.inject(ProfileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getProfile GETs /api/account/profile and returns the response', () => {
    let received: unknown;
    service.getProfile().subscribe(r => (received = r));

    const req = httpMock.expectOne(`${accountUrl}/profile`);
    expect(req.request.method).toBe('GET');
    req.flush({ isSuccess: true, message: '', result: { id: 'u1' } });

    expect(received).toEqual({ isSuccess: true, message: '', result: { id: 'u1' } });
  });

  it('addAddress POSTs the address and reloads the profile on success', () => {
    let received: unknown;
    service.addAddress(address).subscribe(r => (received = r));

    const req = httpMock.expectOne(`${accountUrl}/addresses`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(address);
    req.flush({ isSuccess: true, message: 'Address added', result: { id: 'a1' } });

    expect(received).toEqual({ isSuccess: true, message: 'Address added', result: { id: 'a1' } });
    expect(authService.loadUserProfile).toHaveBeenCalledTimes(1);
  });

  it('addAddress does not reload the profile when the API rejects the address', () => {
    service.addAddress(address).subscribe();

    httpMock.expectOne(`${accountUrl}/addresses`).flush({ isSuccess: false, message: 'Invalid zip code' });

    expect(authService.loadUserProfile).not.toHaveBeenCalled();
  });

  it('changePassword POSTs the request to /api/account/change-password', () => {
    const request: ChangePasswordRequest = { currentPassword: 'old', newPassword: 'N3w!pass', confirmNewPassword: 'N3w!pass' };
    service.changePassword(request).subscribe();

    const req = httpMock.expectOne(`${accountUrl}/change-password`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ isSuccess: true, message: 'Password changed' });
  });

  it('lists the 50 US state codes and the address labels', () => {
    const states = service.getStates();

    expect(states.length).toBe(50);
    expect(new Set(states).size).toBe(50);
    expect(states).toContain('TX');
    expect(service.getAddressLabels()).toEqual(['Home', 'Work', 'Other']);
  });
});
