import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Get token directly from localStorage (NO AuthService injection to avoid circular dependency)
  const token = localStorage.getItem('access_token');
  
  // DEBUG LOGGING
  console.log('🔐 Auth Interceptor:', {
    url: req.url,
    hasToken: !!token,
    tokenPreview: token ? token.substring(0, 20) + '...' : 'NO TOKEN'
  });
  
  if (token) {
    // User is authenticated - add Bearer token
    console.log('✅ Adding Bearer token to request');
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  } else {
    // User is NOT authenticated - add session ID for guest cart
    const sessionId = getOrCreateGuestSession();
    if (sessionId) {
      console.log('✅ Adding Session ID to request:', sessionId);
      req = req.clone({
        setHeaders: {
          'X-Session-Id': sessionId
        }
      });
    } else {
      console.warn('⚠️ No token and no session ID!');
    }
  }
  
  return next(req);
};

/**
 * Get or create guest session ID
 * This is a standalone function to avoid circular dependencies
 */
function getOrCreateGuestSession(): string {
  let sessionId = localStorage.getItem('guest_session_id');
  if (!sessionId) {
    sessionId = 'guest_' + Date.now() + '_' + Math.random().toString(36).substring(2, 15);
    localStorage.setItem('guest_session_id', sessionId);
    console.log('🆕 Created new guest session:', sessionId);
  }
  return sessionId;
}