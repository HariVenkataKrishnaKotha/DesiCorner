import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { ToastrService } from 'ngx-toastr';

import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { OrderService } from '../../core/services/order.service';
import { Cart } from '../../core/models/cart.models';
import { CreateOrderRequest } from '../../core/models/order.models';
import { OtpService } from '@core/services/otp.service';
import { OnDestroy } from '@angular/core';
import { PaymentService } from '@core/services/payment.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatDividerModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatSelectModule
  ],
  templateUrl: './checkout.html',
  styleUrls: ['./checkout.scss']
})
export class CheckoutComponent implements OnInit, OnDestroy {
  private cartService = inject(CartService);
  private authService = inject(AuthService);
  private orderService = inject(OrderService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private toastr = inject(ToastrService);
  private otpService = inject(OtpService);
  private paymentService = inject(PaymentService);

  cart: Cart = { items: [], subtotal: 0, tax: 0, deliveryFee: 0, discount: 0, total: 0 };
  checkoutForm!: FormGroup;
  isSubmitting = false;
  isAuthenticated = false;
  orderError = '';
  // OTP verification state
  otpSent = false;
  otpVerified = false;
  otpSending = false;
  otpVerifying = false;
  otpError = '';
  countdown = 0;
  private countdownInterval?: any;
  // Payment state
  paymentIntentId: string | null = null;
  clientSecret: string | null = null;
  showPaymentForm = false;
  isProcessingPayment = false;
  paymentError = '';

  // US States for dropdown
  states = [
    'AL', 'AK', 'AZ', 'AR', 'CA', 'CO', 'CT', 'DE', 'FL', 'GA',
    'HI', 'ID', 'IL', 'IN', 'IA', 'KS', 'KY', 'LA', 'ME', 'MD',
    'MA', 'MI', 'MN', 'MS', 'MO', 'MT', 'NE', 'NV', 'NH', 'NJ',
    'NM', 'NY', 'NC', 'ND', 'OH', 'OK', 'OR', 'PA', 'RI', 'SC',
    'SD', 'TN', 'TX', 'UT', 'VT', 'VA', 'WA', 'WV', 'WI', 'WY'
  ];

  ngOnInit(): void {
    // Subscribe to cart updates
    this.cartService.cart$.subscribe(cart => {
      this.cart = cart;
    });

    // Check auth state
    this.authService.authState$.subscribe(state => {
      this.isAuthenticated = state.isAuthenticated;
      this.updateFormValidation();
    });

    // Initialize Stripe
this.paymentService.initializeStripe().catch(error => {
  console.error('Failed to initialize Stripe:', error);
  this.toastr.error('Payment system unavailable', 'Error');
});

    // Initialize form
    this.initForm();
  }

  ngOnDestroy(): void {
  if (this.countdownInterval) {
    clearInterval(this.countdownInterval);
  }
  // Cleanup Stripe card element
  this.paymentService.destroyCardElement();
}

  private initForm(): void {
    this.checkoutForm = this.fb.group({
      // Contact info (for guests)
      email: ['', [Validators.email]],
      phone: [''],
      otpCode: ['', [Validators.minLength(6), Validators.maxLength(6)]],
      
      // Delivery address
      fullName: ['', Validators.required],
      street: ['', Validators.required],
      apartment: [''],
      city: ['', Validators.required],
      state: ['', Validators.required],
      zipCode: ['', [Validators.required, Validators.pattern(/^\d{5}(-\d{4})?$/)]],
      
      // Optional
      instructions: [''],
      
      // Payment
      paymentMethod: ['Stripe', Validators.required],
    });

    this.updateFormValidation();
  }

  private updateFormValidation(): void {
  if (!this.checkoutForm) return;

  const emailControl = this.checkoutForm.get('email');
  const phoneControl = this.checkoutForm.get('phone');
  const otpCodeControl = this.checkoutForm.get('otpCode');

  if (!this.isAuthenticated) {
    // Guest checkout - require email, phone, and OTP
    emailControl?.setValidators([Validators.required, Validators.email]);
    phoneControl?.setValidators([Validators.required]);
    otpCodeControl?.setValidators([Validators.required, Validators.minLength(6), Validators.maxLength(6)]);
  } else {
    // Authenticated - clear guest field validators
    emailControl?.clearValidators();
    phoneControl?.clearValidators();
    otpCodeControl?.clearValidators();
  }

  emailControl?.updateValueAndValidity();
  phoneControl?.updateValueAndValidity();
  otpCodeControl?.updateValueAndValidity();
}

sendOtp(): void {
  const emailControl = this.checkoutForm.get('email');
  if (!emailControl?.valid) {
    emailControl?.markAsTouched();
    return;
  }

  this.otpSending = true;
  this.otpError = '';
  const email = emailControl.value;

  this.otpService.sendOtp(email, 'Order Verification').subscribe({
    next: (response) => {
      this.otpSending = false;
      if (response.isSuccess) {
        this.otpSent = true;
        this.startCountdown();
        this.toastr.success('Verification code sent to your email', 'Success');
      } else {
        this.otpError = response.message || 'Failed to send verification code';
        this.toastr.error(this.otpError, 'Error');
      }
    },
    error: (err) => {
      this.otpSending = false;
      this.otpError = 'Failed to send verification code. Please try again.';
      this.toastr.error(this.otpError, 'Error');
      console.error('Send OTP error:', err);
    }
  });
}

verifyOtp(): void {
  const email = this.checkoutForm.get('email')?.value;
  const code = this.checkoutForm.get('otpCode')?.value;

  if (!code || code.length !== 6) {
    this.otpError = 'Please enter a valid 6-digit code';
    return;
  }

  this.otpVerifying = true;
  this.otpError = '';

  this.otpService.verifyOtp(email, code).subscribe({
    next: (response) => {
      this.otpVerifying = false;
      if (response.isSuccess) {
        this.otpVerified = true;
        this.otpError = '';
        
        // Disable the OTP input field after successful verification
        const otpControl = this.checkoutForm.get('otpCode');
        //otpControl?.disable();
        otpControl?.clearValidators();
        otpControl?.updateValueAndValidity();
        
        // Log form validity
        console.log('OTP verified. Form valid:', this.checkoutForm.valid);
        console.log('Form errors:', this.getFormErrors());
        
        this.toastr.success('Email verified successfully!', 'Success');
      } else {
        this.otpError = response.message || 'Invalid verification code';
        this.toastr.error(this.otpError, 'Error');
      }
    },
    error: (err) => {
      this.otpVerifying = false;
      this.otpError = 'Failed to verify code. Please try again.';
      this.toastr.error(this.otpError, 'Error');
      console.error('Verify OTP error:', err);
    }
  });
}

getFormErrors(): any {
  const errors: any = {};
  Object.keys(this.checkoutForm.controls).forEach(key => {
    const control = this.checkoutForm.get(key);
    if (control && control.invalid) {
      errors[key] = control.errors;
    }
  });
  return errors;
}

private startCountdown(): void {
  this.countdown = 120; // 2 minutes
  this.countdownInterval = setInterval(() => {
    this.countdown--;
    if (this.countdown <= 0) {
      clearInterval(this.countdownInterval);
    }
  }, 1000);
}

get countdownDisplay(): string {
  const minutes = Math.floor(this.countdown / 60);
  const seconds = this.countdown % 60;
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

get canPlaceOrder(): boolean {
  const formValid = this.checkoutForm.valid;
  const hasItems = this.cart.items.length > 0;
  const notSubmitting = !this.isSubmitting;
  const guestVerified = this.isAuthenticated || this.otpVerified;

  console.log('Can place order check:', {
    formValid,
    hasItems,
    notSubmitting,
    isAuthenticated: this.isAuthenticated,
    otpVerified: this.otpVerified,
    guestVerified,
    finalResult: formValid && hasItems && notSubmitting && guestVerified
  });

  return formValid && hasItems && notSubmitting && guestVerified;
}

resendOtp(): void {
  this.otpSent = false;
  this.otpVerified = false;
  this.otpError = '';
  this.checkoutForm.get('otpCode')?.reset();
  this.checkoutForm.get('otpCode')?.enable(); // Re-enable the field
  this.sendOtp();
}

  async onSubmit(): Promise<void> {
  // Validate form
  if (this.checkoutForm.invalid || this.cart.items.length === 0) {
    Object.keys(this.checkoutForm.controls).forEach(key => {
      this.checkoutForm.get(key)?.markAsTouched();
    });
    return;
  }

  // For guest checkout, verify OTP is completed
  if (!this.isAuthenticated && !this.otpVerified) {
    this.toastr.warning('Please verify your email with the code sent to you', 'Verification Required');
    return;
  }

  this.isSubmitting = true;
  this.orderError = '';
  this.paymentError = '';

  try {
    // Step 1: Create Payment Intent
    console.log('Creating payment intent for amount:', this.cart.total);
    
    const paymentIntent = await this.paymentService.createPaymentIntent({
      orderId: '00000000-0000-0000-0000-000000000000', // Temporary, will be replaced with actual order ID
      amount: this.cart.total,
      currency: 'usd'
    }).toPromise();

    if (!paymentIntent) {
      throw new Error('Failed to create payment intent');
    }

    this.paymentIntentId = paymentIntent.paymentIntentId;
    this.clientSecret = paymentIntent.clientSecret;

    console.log('Payment intent created:', this.paymentIntentId);

    // Step 2: Show payment form and create card element
    this.showPaymentForm = true;
    this.isSubmitting = false;

    // Wait for DOM to render the card-element container
    setTimeout(() => {
      this.paymentService.createCardElement('card-element');
      this.toastr.info('Please enter your card details', 'Payment');
    }, 100);

  } catch (error: any) {
    this.isSubmitting = false;
    console.error('Error creating payment intent:', error);
    this.paymentError = error.message || 'Failed to initialize payment';
    this.toastr.error(this.paymentError, 'Payment Error');
  }
}

async confirmPayment(): Promise<void> {
  if (!this.clientSecret || !this.paymentIntentId) {
    this.toastr.error('Payment not initialized', 'Error');
    return;
  }

  this.isProcessingPayment = true;
  this.paymentError = '';

  try {
    // Step 3: Confirm payment with Stripe
    console.log('Confirming payment...');
    const result = await this.paymentService.confirmPayment(this.clientSecret);

    if (!result.success) {
      throw new Error(result.error || 'Payment failed');
    }

    console.log('✅ Payment confirmed successfully');
    this.toastr.success('Payment successful!', 'Success');

    // Step 4: Create order with paymentIntentId
    await this.createOrderWithPayment();

  } catch (error: any) {
    this.isProcessingPayment = false;
    console.error('Payment confirmation error:', error);
    this.paymentError = error.message || 'Payment failed';
    this.toastr.error(this.paymentError, 'Payment Failed');
  }
}

private async createOrderWithPayment(): Promise<void> {
  const formValue = this.checkoutForm.value;

  // Build full address
  let address = formValue.street;
  if (formValue.apartment) {
    address += `, ${formValue.apartment}`;
  }

  // Build order request with paymentIntentId
  const orderRequest: CreateOrderRequest = {
    deliveryAddress: address,
    deliveryCity: formValue.city,
    deliveryState: formValue.state,
    deliveryZipCode: formValue.zipCode,
    deliveryInstructions: formValue.instructions || undefined,
    paymentMethod: 'Stripe',
    paymentIntentId: this.paymentIntentId!,
    
    // Add session ID for guest users
    sessionId: this.authService.isAuthenticated ? undefined : this.authService.guestSessionId,
    
    // For guest checkout
    email: this.isAuthenticated ? undefined : formValue.email,
    phone: this.isAuthenticated ? undefined : formValue.phone,
    otpCode: this.isAuthenticated ? undefined : formValue.otpCode
  };

  console.log('Creating order with payment:', orderRequest);

  // Submit order
  this.orderService.createOrder(orderRequest).subscribe({
    next: (response) => {
      this.isProcessingPayment = false;
      
      if (response.isSuccess && response.result) {
        // Clear cart on success
        this.cartService.clearCart();
        
        // Show success message
        this.toastr.success(
          `Order #${response.result.orderNumber} placed successfully!`,
          'Order Confirmed',
          { timeOut: 5000 }
        );
        
        // Navigate to order detail page
        this.router.navigate(['/orders', response.result.id]);
      } else {
        this.orderError = response.message || 'Failed to place order';
        this.toastr.error(this.orderError, 'Order Failed');
      }
    },
    error: (err) => {
      this.isProcessingPayment = false;
      console.error('Order creation error:', err);
      
      this.orderError = err.error?.message || 'Failed to place order. Please try again.';
      this.toastr.error(this.orderError, 'Order Failed');
    }
  });
}

cancelPayment(): void {
  this.showPaymentForm = false;
  this.paymentIntentId = null;
  this.clientSecret = null;
  this.paymentError = '';
  this.paymentService.destroyCardElement();
  this.toastr.info('Payment cancelled', 'Info');
}
}