import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin } from 'rxjs';
import { AdminService } from '@core/services/admin.service';
import { ProductService } from '@core/services/product.service';
import { 
  OrderStats, 
  UserStats, 
  CouponStats, 
  ProductStats,
  AdminOrderListItem,
  RecentUser
} from '@core/models/admin.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class AdminDashboardComponent implements OnInit {
  private adminService = inject(AdminService);
  private productService = inject(ProductService);

  loading = true;
  error = '';

  // Stats
  orderStats: OrderStats | null = null;
  userStats: UserStats | null = null;
  couponStats: CouponStats | null = null;
  productStats: ProductStats | null = null;

  // Recent activity
  recentOrders: AdminOrderListItem[] = [];
  recentUsers: RecentUser[] = [];

  // Basic stats fallback
  totalCategories = 0;

  ngOnInit(): void {
    this.loadAllStats();
  }

  private loadAllStats(): void {
    this.loading = true;

    // Load all stats in parallel
    forkJoin({
      orderStats: this.adminService.getOrderStats(),
      userStats: this.adminService.getUserStats(),
      couponStats: this.adminService.getCouponStats(),
      productStats: this.adminService.getProductStats(),
      recentOrders: this.adminService.getRecentOrders(5),
      recentUsers: this.adminService.getRecentUsers(5),
      categories: this.productService.loadCategories()
    }).subscribe({
      next: (results) => {
        this.loading = false;

        if (results.orderStats.isSuccess) {
          this.orderStats = results.orderStats.result!;
        }

        if (results.userStats.isSuccess) {
          this.userStats = results.userStats.result!;
        }

        if (results.couponStats.isSuccess) {
          this.couponStats = results.couponStats.result!;
        }

        if (results.productStats.isSuccess) {
          this.productStats = results.productStats.result!;
        }

        if (results.recentOrders.isSuccess) {
          this.recentOrders = results.recentOrders.result || [];
        }

        if (results.recentUsers.isSuccess) {
          this.recentUsers = results.recentUsers.result || [];
        }

        if (results.categories.isSuccess) {
          this.totalCategories = results.categories.result?.length || 0;
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = 'Failed to load dashboard data';
        console.error(err);
      }
    });
  }

  refreshData(): void {
    this.loadAllStats();
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getOrderStatusClass(status: string): string {
    const classes: Record<string, string> = {
      'Pending': 'status-pending',
      'Confirmed': 'status-confirmed',
      'Preparing': 'status-preparing',
      'OutForDelivery': 'status-out',
      'Delivered': 'status-delivered',
      'Cancelled': 'status-cancelled'
    };
    return classes[status] || '';
  }
}
