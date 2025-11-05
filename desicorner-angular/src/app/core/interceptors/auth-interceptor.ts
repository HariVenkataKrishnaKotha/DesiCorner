import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  
  // Get the access token
  const token = authService.accessToken;
  
  // Clone the request and add authorization header if token exists
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }
  
  // For cookie-based endpoints, ensure credentials are sent
  if (req.url.includes('/api/account')) {
    req = req.clone({
      withCredentials: true
    });
  }
  
  return next(req);
};