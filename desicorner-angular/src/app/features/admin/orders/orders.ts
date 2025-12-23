import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '@core/services/admin.service';
import { 
  AdminOrderListItem, 
  AdminOrderFilter, 
  OrderStats 
} from '@core/models/admin.models';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './orders.html',
  styleUrl: './orders.scss'
})
export class AdminOrdersComponent implements OnInit {
  private adminService = inject(AdminService);

  orders: AdminOrderListItem[] = [];
  stats: OrderStats | null = null;
  loading = false;
  error = '';

  // Filter state
  filter: AdminOrderFilter = {
    page: 1,
    pageSize: 10,
    sortBy: 'OrderDate',
    sortDescending: true,
    status: '',
    paymentStatus: '',
    searchTerm: ''
  };

  totalCount = 0;
  totalPages = 0;

  // Status options
  statusOptions = ['', 'Pending', 'Confirmed', 'Preparing', 'OutForDelivery', 'Delivered', 'Cancelled'];
  paymentStatusOptions = ['', 'Pending', 'Paid', 'Failed', 'Refunded'];

  // Selected order for detail view
  selectedOrder: any = null;
  showOrderDetail = false;

  ngOnInit(): void {
    this.loadStats();
    this.loadOrders();
  }

  loadStats(): void {
    this.adminService.getOrderStats().subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.stats = response.result!;
        }
      },
      error: (err) => console.error('Failed to load stats', err)
    });
  }

  loadOrders(): void {
    this.loading = true;
    this.error = '';

    this.adminService.getAllOrders(this.filter).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess && response.result) {
          this.orders = response.result.orders;
          this.totalCount = response.result.totalCount;
          this.totalPages = response.result.totalPages;
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = 'Failed to load orders';
        console.error(err);
      }
    });
  }

  onSearch(): void {
    this.filter.page = 1;
    this.loadOrders();
  }

  onFilterChange(): void {
    this.filter.page = 1;
    this.loadOrders();
  }

  onPageChange(page: number): void {
    this.filter.page = page;
    this.loadOrders();
  }

  onSort(column: string): void {
    if (this.filter.sortBy === column) {
      this.filter.sortDescending = !this.filter.sortDescending;
    } else {
      this.filter.sortBy = column;
      this.filter.sortDescending = true;
    }
    this.loadOrders();
  }

  viewOrder(order: AdminOrderListItem): void {
    this.adminService.getOrderById(order.id).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.selectedOrder = response.result;
          this.showOrderDetail = true;
        }
      },
      error: (err) => console.error('Failed to load order details', err)
    });
  }

  closeOrderDetail(): void {
    this.showOrderDetail = false;
    this.selectedOrder = null;
  }

  updateStatus(orderId: string, newStatus: string): void {
    this.adminService.updateOrderStatus(orderId, newStatus).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.loadOrders();
          this.loadStats();
          if (this.selectedOrder?.id === orderId) {
            this.selectedOrder.status = newStatus;
          }
        }
      },
      error: (err) => console.error('Failed to update status', err)
    });
  }

  getStatusClass(status: string): string {
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

  getPaymentStatusClass(status: string): string {
    const classes: Record<string, string> = {
      'Pending': 'payment-pending',
      'Paid': 'payment-paid',
      'Failed': 'payment-failed',
      'Refunded': 'payment-refunded'
    };
    return classes[status] || '';
  }

  get pages(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.filter.page - 2);
    const end = Math.min(this.totalPages, start + 4);
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }
}