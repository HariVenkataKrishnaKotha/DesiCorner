export const environment = {
  production: false,
  gatewayUrl: 'https://localhost:5000',
  authServerUrl: 'https://localhost:7001',
  
  oidc: {
    issuer: 'https://localhost:7001/',
    clientId: 'desicorner-angular',
    redirectUri: window.location.origin + '/auth/callback',
    postLogoutRedirectUri: window.location.origin,
    responseType: 'code',
    scope: 'openid profile email phone offline_access desicorner.products.read desicorner.cart desicorner.orders.read desicorner.orders.write desicorner.payment',
    showDebugInformation: true,
    requireHttps: false, // Dev only!
    strictDiscoveryDocumentValidation: false // Dev only!
  }
};