import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { adminGuard } from './core/guards/admin-guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home-module').then(m => m.HomeModule)
  },
  {
    path: 'products',
    loadChildren: () => import('./features/products/products-module').then(m => m.ProductsModule)
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth-module').then(m => m.AuthModule)
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart-module').then(m => m.CartModule)
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout-module').then(m => m.CheckoutModule),
    canActivate: [authGuard]
  },
  {
    path: 'orders',
    loadChildren: () => import('./features/orders/orders-module').then(m => m.OrdersModule),
    canActivate: [authGuard]
  },
  {
    path: 'profile',
    loadComponent: () => import('./features/profile/profile-module').then(m => m.ProfileModule),
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin-module').then(m => m.AdminModule),
    canActivate: [adminGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];