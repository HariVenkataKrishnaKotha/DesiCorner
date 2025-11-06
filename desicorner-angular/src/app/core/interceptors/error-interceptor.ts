import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An error occurred';
      let showToast = true;

      // Don't show toast for certain endpoints
      const isAuthCheckEndpoint = req.url.includes('/api/account/check-auth') || 
                                   req.url.includes('/api/account/profile');
      
      const isLoginEndpoint = req.url.includes('/api/account/login');

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = `Error: ${error.error.message}`;
      } else {
        // Server-side error
        switch (error.status) {
          case 400:
            errorMessage = error.error?.message || 'Bad request';
            break;
          case 401:
            if (isAuthCheckEndpoint) {
              // Silently fail for auth check - user is just not logged in
              showToast = false;
            } else if (isLoginEndpoint) {
              // Show specific message for login failure
              errorMessage = error.error?.message || 'Invalid email or password';
            } else {
              errorMessage = 'Your session has expired. Please login again.';
              router.navigate(['/auth/login']);
            }
            break;
          case 403:
            errorMessage = 'You do not have permission to perform this action';
            break;
          case 404:
            errorMessage = 'Resource not found';
            break;
          case 500:
            errorMessage = 'Internal server error. Please try again later.';
            break;
          default:
            errorMessage = error.error?.message || `Error: ${error.status}`;
        }
      }

      // Show toast if needed
      if (showToast) {
        toastr.error(errorMessage, 'Error');
      }

      return throwError(() => error);
    })
  );
};