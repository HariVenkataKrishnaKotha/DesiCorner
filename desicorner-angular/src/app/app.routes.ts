import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { adminGuard } from './core/guards/admin-guard';

export const routes: Routes = [
  {
  path: 'products',
  loadComponent: () => import('./features/products/product-list/product-list').then(m => m.ProductListComponent)
},
{
  path: 'products/:id',
  loadComponent: () => import('./features/products/product-detail/product-detail').then(m => m.ProductDetailComponent)
},
  {
  path: 'orders',
  canActivate: [authGuard],
  loadComponent: () => import('./features/orders/order-list').then(m => m.OrderListComponent)
},
{
  path: 'orders/:id',
  canActivate: [authGuard],
  loadComponent: () => import('./features/orders/order-detail').then(m => m.OrderDetailComponent)
},
{
  path: 'profile',
  canActivate: [authGuard],
  loadComponent: () => import('./features/profile/profile').then(m => m.ProfileComponent)
},
  {
    path: '',
    loadComponent: () => import('./features/home/home').then(m => m.HomeComponent)
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth-routing-module').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart').then(m => m.CartComponent)
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout').then(m => m.CheckoutComponent)
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/admin/dashboard/dashboard').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./features/admin/products/products').then(m => m.AdminProductsComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./features/admin/categories/categories').then(m => m.AdminCategoriesComponent)
      },
      {
      path: 'orders',
      loadComponent: () => import('./features/admin/orders/orders').then(m => m.AdminOrdersComponent)
    },
    {
      path: 'users',
      loadComponent: () => import('./features/admin/users/users').then(m => m.AdminUsersComponent)
    },
    {
      path: 'coupons',
      loadComponent: () => import('./features/admin/coupons/coupons').then(m => m.AdminCouponsComponent)
    }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];