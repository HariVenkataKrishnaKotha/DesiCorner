import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/dashboard').then(m => m.AdminDashboardComponent)
  },
  {
    path: 'orders',
    loadComponent: () => import('./orders/orders').then(m => m.AdminOrdersComponent)
  },
  {
    path: 'users',
    loadComponent: () => import('./users/users').then(m => m.AdminUsersComponent)
  },
  {
    path: 'coupons',
    loadComponent: () => import('./coupons/coupons').then(m => m.AdminCouponsComponent)
  },
  {
    path: 'products',
    loadComponent: () => import('./products/products').then(m => m.AdminProductsComponent)
  },
  {
    path: 'categories',
    loadComponent: () => import('./categories/categories').then(m => m.AdminCategoriesComponent)
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }