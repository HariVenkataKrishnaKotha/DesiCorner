export interface Order {
  id: string;
  orderNumber: string;
  userId: string;
  userEmail: string;
  userPhone: string;
  deliveryAddress: string;
  deliveryCity: string;
  deliveryState: string;
  deliveryZipCode: string;
  deliveryInstructions?: string;
  subTotal: number;
  taxAmount: number;
  deliveryFee: number;
  discountAmount: number;
  total: number;
  status: OrderStatus;
  paymentIntentId?: string;
  paymentStatus: PaymentStatus;
  paymentMethod: string;
  orderDate: string;
  items: OrderItem[];
}

export type OrderType = 'Delivery' | 'Pickup';

export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  productImage?: string;
  price: number;
  quantity: number;
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  orderDate: string;
  status: OrderStatus;
  total: number;
  itemCount: number;
}

export interface CreateOrderRequest {
  // Order type
  orderType: OrderType;
  scheduledPickupTime?: string;
  
  // Delivery address (optional for pickup)
  deliveryAddress?: string;
  deliveryCity?: string;
  deliveryState?: string;
  deliveryZipCode?: string;
  deliveryInstructions?: string;
  
  // Payment
  paymentMethod: 'Stripe' | 'PayAtPickup';
  paymentIntentId?: string;
  
  // Guest checkout fields
  email?: string;
  phone?: string;
  otpCode?: string;
  sessionId?: string;
}

export type OrderStatus = 
  | 'Pending'
  | 'Confirmed'
  | 'Preparing'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Cancelled';

export type PaymentStatus = 
  | 'Pending'
  | 'Completed'
  | 'Failed';

export interface PaginatedOrderResponse {
  items: OrderSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}