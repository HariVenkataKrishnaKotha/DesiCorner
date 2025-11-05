export const environment = {
  production: true,
  gatewayUrl: 'https://api.desicorner.com',
  authServerUrl: 'https://auth.desicorner.com',
  
  oidc: {
    issuer: 'https://auth.desicorner.com/',
    clientId: 'desicorner-angular',
    redirectUri: window.location.origin + '/auth/callback',
    postLogoutRedirectUri: window.location.origin,
    responseType: 'code',
    scope: 'openid profile email phone offline_access desicorner.products.read desicorner.cart desicorner.orders.read desicorner.orders.write desicorner.payment',
    showDebugInformation: false,
    requireHttps: true,
    strictDiscoveryDocumentValidation: true
  }
};