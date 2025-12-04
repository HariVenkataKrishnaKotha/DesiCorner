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
 * IMPORTANT: Uses the same key as GuestSessionService to maintain consistency
 */
function getOrCreateGuestSession(): string {
  const SESSION_KEY = 'desicorner_guest_session';
  let sessionId = localStorage.getItem(SESSION_KEY);
  if (!sessionId) {
    // Generate a UUID v4-like session ID (same format as GuestSessionService)
    sessionId = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
    localStorage.setItem(SESSION_KEY, sessionId);
    console.log('🆕 Created new guest session:', sessionId);
  }
  return sessionId;
}