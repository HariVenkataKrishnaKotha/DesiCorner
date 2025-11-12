import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/response.models';

export interface Order {
  id: string;
  orderNumber: string;
  userId: string;
  items: OrderItem[];
  subtotal: number;
  tax: number;
  deliveryFee: number;
  discount: number;
  total: number;
  status: 'Pending' | 'Confirmed' | 'Preparing' | 'OutForDelivery' | 'Delivered' | 'Cancelled';
  deliveryAddress: string;
  estimatedDeliveryTime?: Date;
  createdAt: Date;
  updatedAt?: Date;
}

export interface OrderItem {
  productId: string;
  productName: string;
  quantity: number;
  price: number;
}

export interface CreateOrderRequest {
  items: OrderItem[];
  deliveryAddressId: string;
  couponCode?: string;
  paymentIntentId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private api = inject(ApiService);

  createOrder(request: CreateOrderRequest): Observable<ApiResponse<Order>> {
    return this.api.post<ApiResponse<Order>>('/api/orders', request);
  }

  getOrders(): Observable<ApiResponse<Order[]>> {
    return this.api.get<ApiResponse<Order[]>>('/api/orders');
  }

  getOrderById(id: string): Observable<ApiResponse<Order>> {
    return this.api.get<ApiResponse<Order>>(`/api/orders/${id}`);
  }

  cancelOrder(id: string): Observable<ApiResponse> {
    return this.api.post<ApiResponse>(`/api/orders/${id}/cancel`, {});
  }
}