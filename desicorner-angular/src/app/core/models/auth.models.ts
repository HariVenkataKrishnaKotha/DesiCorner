// OpenIddict Token Response
export interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token: string;
  scope: string;
  id_token?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber: string;
  dietaryPreference: 'Veg' | 'Non-Veg' | 'Vegan';
}

export interface SendOtpRequest {
  email?: string;
  phoneNumber?: string;
  purpose: string;
  deliveryMethod: 'Email' | 'SMS';
}

export interface VerifyOtpRequest {
  identifier: string;
  otp: string;
}

export interface UserProfile {
  id: string;
  email: string;
  phoneNumber: string;
  phoneNumberConfirmed: boolean;
  dietaryPreference: string;
  rewardPoints: number;
  addresses: Address[];
  roles: string[];
}

export interface Address {
  id: string;
  userId: string;
  label: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  isDefault: boolean;
}

export interface AuthState {
  isAuthenticated: boolean;
  userId?: string;
  profile?: UserProfile;
  accessToken?: string;
  loading: boolean;
}