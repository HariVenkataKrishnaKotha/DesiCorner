import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/login').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./register/register').then(m => m.Register)
  },
  {
    path: 'verify-otp',
    loadComponent: () => import('./verify-otp/verify-otp').then(m => m.VerifyOtp)
  },
  {
    path: 'callback',
    loadComponent: () => import('./callback/callback').then(m => m.Callback)
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  }
];