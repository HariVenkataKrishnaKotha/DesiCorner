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
  // Guest checkout fields (optional - only for non-authenticated users)
  email?: string;
  phone?: string;
  otpCode?: string;
  
  // Delivery address (required)
  deliveryAddress: string;
  deliveryCity: string;
  deliveryState: string;
  deliveryZipCode: string;
  deliveryInstructions?: string;
  
  // Payment
  paymentMethod: string;
  paymentIntentId?: string;
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