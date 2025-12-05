export interface PaymentIntentRequest {
  orderId: string;
  amount: number;
  currency: string;
  metadata?: { [key: string]: string };
}

export interface PaymentIntentResponse {
  paymentIntentId: string;
  clientSecret: string;
  status: string;
  amount: number;
}

export interface VerifyPaymentRequest {
  paymentIntentId: string;
}

export interface VerifyPaymentResponse {
  isSuccess: boolean;
  status: string;
  amount: number;
  errorMessage?: string;
}

export interface StripeConfig {
  publishableKey: string;
}