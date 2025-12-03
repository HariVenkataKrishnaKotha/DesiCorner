import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { OrderService } from '../../core/services/order.service';
import { AuthService } from '../../core/services/auth.service';
import { OrderSummary } from '../../core/models/order.models';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './order-list.html',
  styleUrls: ['./order-list.scss']
})
export class OrderListComponent implements OnInit {
  private orderService = inject(OrderService);
  private authService = inject(AuthService);
  private router = inject(Router);

  orders: OrderSummary[] = [];
  loading = true;
  error = '';
  isAuthenticated = false;

  currentPage = 1;
  pageSize = 10;
  hasMore = true;

  ngOnInit(): void {
    // Check authentication
    this.authService.authState$.subscribe(state => {
      this.isAuthenticated = state.isAuthenticated;
      
      if (!state.isAuthenticated && !state.loading) {
        this.router.navigate(['/auth/login'], { queryParams: { returnUrl: '/orders' } });
      } else if (state.isAuthenticated) {
        this.loadOrders();
      }
    });
  }

  loadOrders(): void {
    this.loading = true;
    this.error = '';

    this.orderService.getMyOrders(this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess && response.result) {
          this.orders = response.result;
          this.hasMore = response.result.length === this.pageSize;
        } else {
          this.error = response.message || 'Failed to load orders';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to load orders';
      }
    });
  }

  loadMore(): void {
    this.currentPage++;
    this.loading = true;

    this.orderService.getMyOrders(this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess && response.result) {
          this.orders = [...this.orders, ...response.result];
          this.hasMore = response.result.length === this.pageSize;
        }
      },
      error: (err) => {
        this.loading = false;
        this.currentPage--; // Revert page increment on error
      }
    });
  }

  getStatusDisplay(status: string): string {
    return this.orderService.getStatusDisplay(status);
  }

  getStatusColor(status: string): string {
    return this.orderService.getStatusColor(status);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}