# desicorner-angular

**Angular 20 SPA with Material Design, NgRx state management, OAuth 2.0 PKCE authentication, and Stripe Elements payment integration for the DesiCorner e-commerce platform.**

[![Angular 20](https://img.shields.io/badge/Angular-20-DD0031?style=flat-square&logo=angular&logoColor=white)]()
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?style=flat-square&logo=typescript&logoColor=white)]()
[![NgRx](https://img.shields.io/badge/NgRx-20.1-BA2BD2?style=flat-square)]()
[![Port](https://img.shields.io/badge/Port-4200-orange?style=flat-square)]()

---

## Role in the System

The Angular SPA is the customer-facing storefront and admin dashboard. It communicates **exclusively through the YARP API Gateway** (`https://localhost:5000`) — never directly to backend services. Uses `angular-oauth2-oidc` for OAuth 2.0 Authorization Code + PKCE flow, NgRx for predictable state management, and `ngx-stripe` for PCI-compliant payment forms.

```
Angular SPA (:4200) ──[All requests]──> YARP Gateway (:5000) ──> Backend Services
```

> 📖 For the overall system architecture, see the [root README](../README.md).

---

## OIDC Configuration

| Setting | Value |
|---------|-------|
| Issuer | `https://localhost:7001/` (dev) / `https://auth.desicorner.com/` (prod) |
| Client ID | `desicorner-angular` |
| Response Type | `code` (Authorization Code + PKCE) |
| Redirect URI | `{origin}/auth/callback` |
| Scopes | `openid profile email phone offline_access desicorner.products.read desicorner.cart desicorner.orders.read desicorner.orders.write desicorner.payment` |

---

## Architecture

| Layer | Purpose | Location |
|-------|---------|----------|
| **Core** | Singleton services, guards, interceptors, models | `src/app/core/` |
| **Features** | Lazy-loaded page modules | `src/app/features/` |
| **Shared** | Reusable UI components | `src/app/shared/` |

### Feature Modules

| Module | Route | Key Components | Description |
|--------|-------|----------------|-------------|
| Home | `/` | HomeComponent | Product listing with categories, search, ratings |
| Auth | `/auth/*` | Login, Register, VerifyOtp, Callback | OAuth 2.0 PKCE authentication flow |
| Products | `/products/*` | ProductList, ProductDetail | Product browsing, filtering, reviews |
| Cart | `/cart` | CartComponent | Cart management, coupon application |
| Checkout | `/checkout` | CheckoutComponent | Delivery/pickup selection, Stripe payment |
| Orders | `/orders/*` | OrderList, OrderDetail | Order history and tracking |
| Profile | `/profile` | ProfileComponent | User profile, delivery addresses, password |
| Admin | `/admin/*` | Dashboard, Products, Categories, Coupons, Orders, Users | Full admin panel with analytics |

### Services

| Service | Purpose |
|---------|---------|
| `AuthService` | OAuth 2.0 login/logout, JWT token management |
| `ProductService` | Product catalog API calls |
| `CartService` | Cart CRUD operations |
| `OrderService` | Order creation, history |
| `PaymentService` | Stripe payment integration |
| `AdminService` | Admin dashboard API calls |
| `ReviewService` | Review CRUD and voting |
| `ProfileService` | User profile, address management |
| `GuestSessionService` | UUID-based guest session tracking |
| `OtpService` | OTP verification API calls |
| `ApiService` | Generic HTTP client with error handling |

### State Management (NgRx)

| Store Slice | Key Actions | Purpose |
|------------|-------------|---------|
| Auth | Login, Logout, LoadProfile | User authentication state + JWT tokens |
| Cart | AddItem, RemoveItem, UpdateQuantity, ApplyCoupon, ClearCart | Shopping cart state |
| Products | LoadProducts, LoadCategories, FilterProducts | Product catalog state |

### Route Guards

| Guard | Purpose |
|-------|---------|
| `AuthGuard` | Protects routes requiring authentication (cart, checkout, orders, profile) |
| `AdminGuard` | Protects admin routes — requires Admin role in JWT claims |

### Interceptors

| Interceptor | Purpose |
|-------------|---------|
| `AuthInterceptor` | Attaches JWT Bearer token to all outgoing API requests |
| `ErrorInterceptor` | Global error handling — extracts error messages, shows toast notifications |

---

## Production Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `@angular/core` | ^20.3.0 | Framework core — components, DI, change detection, signals |
| `@angular/router` | ^20.3.0 | Client-side routing with lazy loading and guards |
| `@angular/forms` | ^20.3.0 | Template-driven and reactive forms |
| `@angular/material` | ^20.2.11 | Material Design UI components |
| `@angular/cdk` | ^20.2.11 | Component Dev Kit — accessibility, overlays, drag-drop |
| `@angular/animations` | ^20.3.10 | Material animations and route transitions |
| `@angular/common` | ^20.3.0 | Core utilities (HttpClient, pipes, directives) |
| `@angular/compiler` | ^20.3.0 | Template compilation |
| `@angular/platform-browser` | ^20.3.0 | DOM rendering and sanitization |
| `@ngrx/store` | ^20.1.0 | Redux-inspired state management |
| `@ngrx/effects` | ^20.1.0 | Side-effect management for async operations |
| `@ngrx/store-devtools` | ^20.1.0 | Redux DevTools integration (dev only) |
| `angular-oauth2-oidc` | ^20.0.2 | OAuth 2.0 / OIDC client (PKCE, token refresh, silent renew) |
| `@stripe/stripe-js` | ^8.5.3 | Stripe SDK for client-side payment element rendering |
| `ngx-stripe` | ^21.8.0 | Angular wrapper for Stripe Elements |
| `ngx-toastr` | ^19.1.0 | Toast notification library |
| `rxjs` | ~7.8.0 | Reactive programming |
| `crypto-js` | ^4.2.0 | Client-side cryptographic utilities |
| `uuid` | ^13.0.0 | UUID generation for idempotency keys and guest sessions |
| `tslib` | ^2.3.0 | TypeScript runtime helpers |
| `zone.js` | ~0.15.0 | Angular change detection mechanism |

### Dev Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `@angular/build` | ^20.3.1 | esbuild-based build system |
| `@angular/cli` | ^20.3.1 | CLI for scaffolding, serving, building, testing |
| `@angular/compiler-cli` | ^20.3.0 | AOT compiler for production builds |
| `typescript` | ~5.9.2 | TypeScript compiler |
| `jasmine-core` | ~5.9.0 | BDD testing framework |
| `karma` | ~6.4.0 | Test runner |
| `karma-chrome-launcher` | ~3.2.0 | Chrome launcher for Karma |
| `karma-coverage` | ~2.2.0 | Istanbul code coverage |
| `karma-jasmine` | ~5.1.0 | Jasmine adapter for Karma |
| `karma-jasmine-html-reporter` | ~2.1.0 | HTML test reporter |
| `@types/crypto-js` | ^4.2.2 | Type definitions |
| `@types/jasmine` | ~5.1.0 | Type definitions |
| `@types/node` | ^24.10.0 | Type definitions |
| `@types/uuid` | ^10.0.0 | Type definitions |

---

## Configuration

### Development (`environment.ts`)

| Setting | Value |
|---------|-------|
| `apiUrl` | `https://localhost:5000` (Gateway) |
| `authServerUrl` | `https://localhost:7001` |
| `stripePublishableKey` | `pk_test_...` |
| `oidcClientId` | `desicorner-angular` |

### Production (`environment.prod.ts`)

| Setting | Value |
|---------|-------|
| `apiUrl` | `https://api.desicorner.com` |
| `authServerUrl` | `https://auth.desicorner.com` |

---

## Running

```bash
cd desicorner-angular

# Install dependencies
npm install

# Development server with hot reload
ng serve
# Open http://localhost:4200

# Production build
ng build --configuration production
# Output: dist/desicorner-angular/

# Run unit tests
ng test

# Run tests with code coverage
ng test --code-coverage
```

**Dependencies:** Requires the YARP Gateway (`https://localhost:5000`) and at minimum:
- **AuthServer** (`:7001`) — for authentication
- **ProductAPI** (`:7101`) — for product listing on the homepage

All other features require their respective backend services to be running.

---

## Folder Structure

```
desicorner-angular/
└── src/
    ├── app/
    │   ├── core/
    │   │   ├── guards/
    │   │   │   ├── admin-guard.spec.ts
    │   │   │   ├── admin-guard.ts
    │   │   │   ├── auth-guard.spec.ts
    │   │   │   └── auth-guard.ts
    │   │   ├── interceptors/
    │   │   │   ├── auth-interceptor.spec.ts
    │   │   │   ├── auth-interceptor.ts
    │   │   │   ├── error-interceptor.spec.ts
    │   │   │   └── error-interceptor.ts
    │   │   ├── models/
    │   │   │   ├── admin.models.ts
    │   │   │   ├── auth.models.ts
    │   │   │   ├── cart.models.ts
    │   │   │   ├── order.models.ts
    │   │   │   ├── payment.models.ts
    │   │   │   ├── product.models.ts
    │   │   │   ├── profile.models.ts
    │   │   │   ├── response.models.ts
    │   │   │   └── review.models.ts
    │   │   ├── services/
    │   │   │   ├── admin.service.ts
    │   │   │   ├── api.service.ts
    │   │   │   ├── api.spec.ts
    │   │   │   ├── api.ts
    │   │   │   ├── auth.service.ts
    │   │   │   ├── auth.spec.ts
    │   │   │   ├── auth.ts
    │   │   │   ├── cart.service.ts
    │   │   │   ├── cart.spec.ts
    │   │   │   ├── cart.ts
    │   │   │   ├── guest-session.service.ts
    │   │   │   ├── order.service.ts
    │   │   │   ├── order.spec.ts
    │   │   │   ├── order.ts
    │   │   │   ├── otp.service.ts
    │   │   │   ├── payment.service.ts
    │   │   │   ├── product.service.ts
    │   │   │   ├── product.spec.ts
    │   │   │   ├── product.ts
    │   │   │   ├── profile.service.ts
    │   │   │   └── review.service.ts
    │   │   └── core-module.ts
    │   ├── features/
    │   │   ├── admin/
    │   │   │   ├── categories/
    │   │   │   │   ├── categories.html
    │   │   │   │   ├── categories.scss
    │   │   │   │   └── categories.ts
    │   │   │   ├── coupons/
    │   │   │   │   ├── coupons.html
    │   │   │   │   ├── coupons.scss
    │   │   │   │   └── coupons.ts
    │   │   │   ├── dashboard/
    │   │   │   │   ├── dashboard.html
    │   │   │   │   ├── dashboard.scss
    │   │   │   │   └── dashboard.ts
    │   │   │   ├── orders/
    │   │   │   │   ├── orders.html
    │   │   │   │   ├── orders.scss
    │   │   │   │   └── orders.ts
    │   │   │   ├── products/
    │   │   │   │   ├── products.html
    │   │   │   │   ├── products.scss
    │   │   │   │   └── products.ts
    │   │   │   ├── users/
    │   │   │   │   ├── users.html
    │   │   │   │   ├── users.scss
    │   │   │   │   └── users.ts
    │   │   │   ├── admin-module.ts
    │   │   │   └── admin-routing-module.ts
    │   │   ├── auth/
    │   │   │   ├── callback/
    │   │   │   │   ├── callback.html
    │   │   │   │   ├── callback.scss
    │   │   │   │   └── callback.ts
    │   │   │   ├── login/
    │   │   │   │   ├── login.html
    │   │   │   │   ├── login.scss
    │   │   │   │   └── login.ts
    │   │   │   ├── register/
    │   │   │   │   ├── register.html
    │   │   │   │   ├── register.scss
    │   │   │   │   └── register.ts
    │   │   │   ├── verify-otp/
    │   │   │   │   ├── verify-otp.html
    │   │   │   │   ├── verify-otp.scss
    │   │   │   │   └── verify-otp.ts
    │   │   │   └── auth-routing-module.ts
    │   │   ├── cart/
    │   │   │   ├── cart-module.ts
    │   │   │   ├── cart-routing-module.ts
    │   │   │   ├── cart.html
    │   │   │   ├── cart.scss
    │   │   │   └── cart.ts
    │   │   ├── checkout/
    │   │   │   ├── checkout-module.ts
    │   │   │   ├── checkout-routing-module.ts
    │   │   │   ├── checkout.html
    │   │   │   ├── checkout.scss
    │   │   │   └── checkout.ts
    │   │   ├── home/
    │   │   │   ├── home-module.ts
    │   │   │   ├── home-routing-module.ts
    │   │   │   ├── home.html
    │   │   │   ├── home.scss
    │   │   │   └── home.ts
    │   │   ├── orders/
    │   │   │   ├── order-detail.html
    │   │   │   ├── order-detail.scss
    │   │   │   ├── order-detail.ts
    │   │   │   ├── order-list.html
    │   │   │   ├── order-list.scss
    │   │   │   ├── order-list.ts
    │   │   │   ├── orders-module.ts
    │   │   │   └── orders-routing-module.ts
    │   │   ├── products/
    │   │   │   ├── product-detail/
    │   │   │   │   ├── product-detail.html
    │   │   │   │   ├── product-detail.scss
    │   │   │   │   └── product-detail.ts
    │   │   │   ├── product-list/
    │   │   │   │   ├── product-list.html
    │   │   │   │   ├── product-list.scss
    │   │   │   │   └── product-list.ts
    │   │   │   ├── products-module.ts
    │   │   │   └── products-routing-module.ts
    │   │   └── profile/
    │   │       ├── profile-module.ts
    │   │       ├── profile-routing-module.ts
    │   │       ├── profile.html
    │   │       ├── profile.scss
    │   │       └── profile.ts
    │   ├── shared/
    │   │   ├── components/
    │   │   │   ├── footer/
    │   │   │   │   ├── footer.html
    │   │   │   │   ├── footer.scss
    │   │   │   │   └── footer.ts
    │   │   │   ├── header/
    │   │   │   │   ├── header.html
    │   │   │   │   ├── header.scss
    │   │   │   │   └── header.ts
    │   │   │   ├── review-form/
    │   │   │   │   └── review-form.ts
    │   │   │   ├── review-item/
    │   │   │   │   └── review-item.ts
    │   │   │   ├── review-list/
    │   │   │   │   └── review-list.ts
    │   │   │   ├── review-summary/
    │   │   │   │   └── review-summary.ts
    │   │   │   └── star-rating/
    │   │   │       └── star-rating.ts
    │   │   └── shared-module.ts
    │   ├── app.config.ts
    │   ├── app.html
    │   ├── app.routes.ts
    │   ├── app.scss
    │   ├── app.spec.ts
    │   └── app.ts
    ├── environments/
    │   ├── environment.prod.ts
    │   └── environment.ts
    ├── index.html
    ├── main.ts
    └── styles.scss
```
