import { createActionGroup, emptyProps, props } from '@ngrx/store';
import {
  RegisterRequest,
  SendOtpRequest,
  VerifyOtpRequest,
  UserProfile,
} from '../../core/models/auth.models';
import { ApiResponse } from '../../core/models/response.models';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    // Initialization
    'Check Auth': emptyProps(),
    'Check Auth Success': emptyProps(),
    'Check Auth No Token': emptyProps(),

    // PKCE Login (Authorization Code + PKCE flow)
    'Pkce Callback Success': emptyProps(),

    // Login Failure (shown in UI for error display)
    'Login Failure': props<{ error: string }>(),

    // Load Profile
    'Load User Profile': emptyProps(),
    'Load User Profile Success': props<{ profile: UserProfile }>(),
    'Load User Profile Failure': props<{ error: string }>(),

    // Logout
    'Logout': emptyProps(),

    // Register
    'Register': props<{ request: RegisterRequest }>(),
    'Register Success': props<{ response: ApiResponse }>(),
    'Register Failure': props<{ error: string }>(),

    // OTP
    'Send Otp': props<{ request: SendOtpRequest }>(),
    'Send Otp Success': props<{ response: ApiResponse }>(),
    'Send Otp Failure': props<{ error: string }>(),

    'Verify Otp': props<{ request: VerifyOtpRequest }>(),
    'Verify Otp Success': props<{ response: ApiResponse }>(),
    'Verify Otp Failure': props<{ error: string }>(),

    // Token Refresh (failure triggers logout, handled by angular-oauth2-oidc for refresh)
    'Refresh Token Failure': props<{ error: string }>(),
  },
});
