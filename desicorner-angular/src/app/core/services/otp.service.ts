import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/response.models';

@Injectable({
  providedIn: 'root'
})
export class OtpService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.gatewayUrl}/api/account`;

  sendOtp(email: string, purpose: string = 'Order Verification'): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/send-otp`, {
      email,
      purpose,
      deliveryMethod: 'Email'
    });
  }

  verifyOtp(email: string, otp: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/verify-otp`, {
      identifier: email,
      otp
    });
  }
}