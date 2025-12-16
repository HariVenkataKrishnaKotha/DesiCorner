import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/response.models';
import { 
  UserProfile, 
  DeliveryAddress, 
  AddAddressRequest, 
  ChangePasswordRequest 
} from '../models/profile.models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly baseUrl = `${environment.gatewayUrl}/api/account`;

  /**
   * Get current user's profile
   * Includes addresses, roles, reward points, etc.
   */
  getProfile(): Observable<ApiResponse<UserProfile>> {
    return this.http.get<ApiResponse<UserProfile>>(`${this.baseUrl}/profile`);
  }

  /**
   * Add a new delivery address
   * @param address - The address details to add
   */
  addAddress(address: AddAddressRequest): Observable<ApiResponse<DeliveryAddress>> {
    return this.http.post<ApiResponse<DeliveryAddress>>(
      `${this.baseUrl}/addresses`,
      address
    ).pipe(
      tap(response => {
        if (response.isSuccess) {
          // Reload user profile to get updated addresses
          this.authService.loadUserProfile();
        }
      })
    );
  }

  /**
   * Change user's password
   * @param request - Current and new password
   */
  changePassword(request: ChangePasswordRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(
      `${this.baseUrl}/change-password`,
      request
    );
  }

  /**
   * Get list of US states for address form dropdown
   */
  getStates(): string[] {
    return [
      'AL', 'AK', 'AZ', 'AR', 'CA', 'CO', 'CT', 'DE', 'FL', 'GA',
      'HI', 'ID', 'IL', 'IN', 'IA', 'KS', 'KY', 'LA', 'ME', 'MD',
      'MA', 'MI', 'MN', 'MS', 'MO', 'MT', 'NE', 'NV', 'NH', 'NJ',
      'NM', 'NY', 'NC', 'ND', 'OH', 'OK', 'OR', 'PA', 'RI', 'SC',
      'SD', 'TN', 'TX', 'UT', 'VT', 'VA', 'WA', 'WV', 'WI', 'WY'
    ];
  }

  /**
   * Get address label suggestions
   */
  getAddressLabels(): string[] {
    return ['Home', 'Work', 'Other'];
  }
}