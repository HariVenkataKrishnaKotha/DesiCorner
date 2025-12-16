export type { UserProfile, Address as DeliveryAddress } from './auth.models';

// Request DTOs for profile operations
export interface AddAddressRequest {
  label: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  zipCode: string;
  isDefault: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface UpdateProfileRequest {
  phoneNumber?: string;
  dietaryPreference?: string;
}