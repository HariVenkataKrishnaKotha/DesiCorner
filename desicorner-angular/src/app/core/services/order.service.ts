import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/response.models';
import { Order, OrderSummary, CreateOrderRequest } from '../models/order.models';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.gatewayUrl}/api/orders`;

  /**
   * Create a new order from the current cart
   */
  createOrder(request: CreateOrderRequest): Observable<ApiResponse<Order>> {
    return this.http.post<ApiResponse<Order>>(this.baseUrl, request);
  }

  /**
   * Get order by ID
   */
  getOrderById(orderId: string): Observable<ApiResponse<Order>> {
    return this.http.get<ApiResponse<Order>>(`${this.baseUrl}/${orderId}`);
  }

  /**
   * Get order by order number
   */
  getOrderByNumber(orderNumber: string): Observable<ApiResponse<Order>> {
    return this.http.get<ApiResponse<Order>>(`${this.baseUrl}/number/${orderNumber}`);
  }

  /**
   * Get current user's orders (paginated)
   */
  getMyOrders(page: number = 1, pageSize: number = 10): Observable<ApiResponse<OrderSummary[]>> {
    return this.http.get<ApiResponse<OrderSummary[]>>(
      `${this.baseUrl}/my-orders?page=${page}&pageSize=${pageSize}`
    );
  }

  /**
   * Cancel an order
   */
  cancelOrder(orderId: string): Observable<ApiResponse<Order>> {
    return this.http.post<ApiResponse<Order>>(`${this.baseUrl}/${orderId}/cancel`, {});
  }

  /**
   * Get status display text
   */
  getStatusDisplay(status: string): string {
    const statusMap: Record<string, string> = {
      'Pending': 'Order Placed',
      'Confirmed': 'Confirmed',
      'Preparing': 'Preparing',
      'OutForDelivery': 'Out for Delivery',
      'Delivered': 'Delivered',
      'Cancelled': 'Cancelled'
    };
    return statusMap[status] || status;
  }

  /**
   * Get status color class for styling
   */
  getStatusColor(status: string): string {
    const colorMap: Record<string, string> = {
      'Pending': 'status-pending',
      'Confirmed': 'status-confirmed',
      'Preparing': 'status-preparing',
      'OutForDelivery': 'status-delivery',
      'Delivered': 'status-delivered',
      'Cancelled': 'status-cancelled'
    };
    return colorMap[status] || 'status-default';
  }
}