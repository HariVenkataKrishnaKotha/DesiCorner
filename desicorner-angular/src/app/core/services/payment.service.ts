import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, from, throwError } from 'rxjs';
import { map, switchMap, catchError } from 'rxjs/operators';
import { loadStripe, Stripe, StripeElements, StripeCardElement } from '@stripe/stripe-js';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/response.models';
import { 
  PaymentIntentRequest, 
  PaymentIntentResponse,
  StripeConfig 
} from '../models/payment.models';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private http = inject(HttpClient);
  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;
  private cardElement: StripeCardElement | null = null;

  /**
   * Initialize Stripe.js with publishable key from backend
   */
  async initializeStripe(): Promise<void> {
    try {
      // Get Stripe publishable key from PaymentAPI
      const config = await this.http
        .get<ApiResponse<StripeConfig>>(`${environment.gatewayUrl}/api/payment/config`)
        .toPromise();

      if (!config?.isSuccess || !config.result?.publishableKey) {
        throw new Error('Failed to get Stripe configuration');
      }

      // Load Stripe.js
      this.stripe = await loadStripe(config.result.publishableKey);

      if (!this.stripe) {
        throw new Error('Failed to load Stripe.js');
      }

      console.log('✅ Stripe initialized successfully');
    } catch (error) {
      console.error('❌ Failed to initialize Stripe:', error);
      throw error;
    }
  }

  /**
   * Create Stripe Elements (card input UI components)
   */
  createCardElement(containerElementId: string): void {
    if (!this.stripe) {
      throw new Error('Stripe not initialized. Call initializeStripe() first.');
    }

    // Create Elements instance
    this.elements = this.stripe.elements();

    // Create and mount card element
    this.cardElement = this.elements.create('card', {
      style: {
        base: {
          fontSize: '16px',
          color: '#32325d',
          fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
          '::placeholder': {
            color: '#aab7c4'
          }
        },
        invalid: {
          color: '#fa755a',
          iconColor: '#fa755a'
        }
      },
      hidePostalCode: true // We collect shipping address separately
    });

    const container = document.getElementById(containerElementId);
    if (container) {
      this.cardElement.mount(`#${containerElementId}`);
      console.log('✅ Card element mounted');
    } else {
      console.error(`❌ Container element #${containerElementId} not found`);
    }
  }

  /**
   * Create payment intent on backend
   */
  createPaymentIntent(request: PaymentIntentRequest): Observable<PaymentIntentResponse> {
    return this.http
      .post<ApiResponse<PaymentIntentResponse>>(
        `${environment.gatewayUrl}/api/payment/create-intent`,
        request
      )
      .pipe(
        map(response => {
          if (!response.isSuccess || !response.result) {
            throw new Error(response.message || 'Failed to create payment intent');
          }
          return response.result;
        }),
        catchError(error => {
          console.error('Error creating payment intent:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Confirm payment with Stripe using card element
   */
  async confirmPayment(clientSecret: string): Promise<{ success: boolean; error?: string }> {
    if (!this.stripe || !this.cardElement) {
      return { success: false, error: 'Stripe not initialized or card element not created' };
    }

    try {
      const { error, paymentIntent } = await this.stripe.confirmCardPayment(clientSecret, {
        payment_method: {
          card: this.cardElement
        }
      });

      if (error) {
        console.error('Payment confirmation error:', error);
        return { success: false, error: error.message };
      }

      if (paymentIntent?.status === 'succeeded') {
        console.log('✅ Payment succeeded:', paymentIntent.id);
        return { success: true };
      }

      return { success: false, error: `Payment status: ${paymentIntent?.status}` };
    } catch (error: any) {
      console.error('Payment confirmation exception:', error);
      return { success: false, error: error.message || 'Payment failed' };
    }
  }

  /**
   * Cleanup - destroy card element
   */
  destroyCardElement(): void {
    if (this.cardElement) {
      this.cardElement.destroy();
      this.cardElement = null;
      console.log('Card element destroyed');
    }
  }

  /**
   * Get the Stripe instance (for advanced use cases)
   */
  getStripe(): Stripe | null {
    return this.stripe;
  }
}