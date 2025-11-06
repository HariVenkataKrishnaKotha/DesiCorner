import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // For cookie-based authentication, just ensure credentials are sent
  // No need to add Authorization header since we're using cookies
  
  if (req.url.includes('/api/')) {
    req = req.clone({
      withCredentials: true
    });
  }
  
  return next(req);
};