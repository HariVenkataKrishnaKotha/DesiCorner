import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ToastrService } from 'ngx-toastr';

import { OrderService } from '../../core/services/order.service';
import { Order } from '../../core/models/order.models';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './order-detail.html',
  styleUrls: ['./order-detail.scss']
})
export class OrderDetailComponent implements OnInit {
  route = inject(ActivatedRoute);
  private router = inject(Router);
  private orderService = inject(OrderService);
  private toastr = inject(ToastrService);

  order?: Order;
  loading = true;
  error = '';
  cancelling = false;

  ngOnInit(): void {
    const orderId = this.route.snapshot.paramMap.get('id');
    if (orderId) {
      this.loadOrder(orderId);
    } else {
      this.error = 'Order ID not provided';
      this.loading = false;
    }
  }

  private loadOrder(orderId: string): void {
    this.loading = true;
    this.error = '';

    this.orderService.getOrderById(orderId).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess && response.result) {
          this.order = response.result;
        } else {
          this.error = response.message || 'Failed to load order';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to load order';
      }
    });
  }

  cancelOrder(): void {
    if (!this.order || !this.canCancelOrder()) return;

    if (!confirm('Are you sure you want to cancel this order?')) {
      return;
    }

    this.cancelling = true;

    this.orderService.cancelOrder(this.order.id).subscribe({
      next: (response) => {
        this.cancelling = false;
        if (response.isSuccess && response.result) {
          this.order = response.result;
          this.toastr.success('Order cancelled successfully', 'Order Cancelled');
        } else {
          this.toastr.error(response.message || 'Failed to cancel order', 'Error');
        }
      },
      error: (err) => {
        this.cancelling = false;
        this.toastr.error(err.error?.message || 'Failed to cancel order', 'Error');
      }
    });
  }

  canCancelOrder(): boolean {
    if (!this.order) return false;
    return this.order.status === 'Pending' || this.order.status === 'Confirmed';
  }

  getStatusDisplay(status: string): string {
    return this.orderService.getStatusDisplay(status);
  }

  getStatusColor(status: string): string {
    return this.orderService.getStatusColor(status);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}