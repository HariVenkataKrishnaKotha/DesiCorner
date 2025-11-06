import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';

export const routes: Routes = [
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
    path: 'profile',
    loadComponent: () => import('./features/profile/profile-module').then(m => m.ProfileModule),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];