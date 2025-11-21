import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  
  // Get the JWT token
  const token = authService.accessToken;
  
  // Clone the request and add authorization header if token exists
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  } else {
    // For guest users, send session ID
    const sessionId = authService.guestSessionId;
    if (sessionId) {
      req = req.clone({
        setHeaders: {
          'X-Session-Id': sessionId
        }
      });
    }
  }
  
  return next(req);
};