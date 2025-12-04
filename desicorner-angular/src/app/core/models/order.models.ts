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
  deliveryAddress: string;
  deliveryCity: string;
  deliveryState: string;
  deliveryZipCode: string;
  deliveryInstructions?: string;
  paymentMethod: string;
  paymentIntentId?: string;
  
  // Guest checkout fields
  email?: string;
  phone?: string;
  otpCode?: string;
  sessionId?: string;  // Add this line
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