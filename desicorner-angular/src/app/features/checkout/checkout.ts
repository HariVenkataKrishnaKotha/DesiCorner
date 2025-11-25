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
export class CheckoutComponent implements OnInit {
  private cartService = inject(CartService);
  private authService = inject(AuthService);
  private orderService = inject(OrderService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  cart: Cart = { items: [], subtotal: 0, tax: 0, deliveryFee: 0, discount: 0, total: 0 };
  checkoutForm!: FormGroup;
  isSubmitting = false;
  isAuthenticated = false;
  orderError = '';

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

    // Initialize form
    this.initForm();
  }

  private initForm(): void {
    this.checkoutForm = this.fb.group({
      // Contact info (for guests)
      email: ['', [Validators.email]],
      phone: [''],
      
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
      paymentMethod: ['CashOnDelivery', Validators.required]
    });

    this.updateFormValidation();
  }

  private updateFormValidation(): void {
    if (!this.checkoutForm) return;

    const emailControl = this.checkoutForm.get('email');
    const phoneControl = this.checkoutForm.get('phone');

    if (!this.isAuthenticated) {
      emailControl?.setValidators([Validators.required, Validators.email]);
      phoneControl?.setValidators([Validators.required]);
    } else {
      emailControl?.clearValidators();
      phoneControl?.clearValidators();
    }

    emailControl?.updateValueAndValidity();
    phoneControl?.updateValueAndValidity();
  }

  onSubmit(): void {
    if (this.checkoutForm.invalid || this.cart.items.length === 0) {
      // Mark all fields as touched to show validation errors
      Object.keys(this.checkoutForm.controls).forEach(key => {
        this.checkoutForm.get(key)?.markAsTouched();
      });
      return;
    }

    // Check if user is authenticated
    if (!this.isAuthenticated) {
      this.toastr.warning('Please login to place an order', 'Authentication Required');
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: '/checkout' } });
      return;
    }

    this.isSubmitting = true;
    this.orderError = '';

    const formValue = this.checkoutForm.value;

    // Build address string
    let address = formValue.street;
    if (formValue.apartment) {
      address += `, ${formValue.apartment}`;
    }

    // Create order request
    const orderRequest: CreateOrderRequest = {
      deliveryAddress: address,
      deliveryCity: formValue.city,
      deliveryState: formValue.state,
      deliveryZipCode: formValue.zipCode,
      deliveryInstructions: formValue.instructions || undefined,
      paymentMethod: formValue.paymentMethod
    };

    this.orderService.createOrder(orderRequest).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.isSuccess && response.result) {
          // Clear cart after successful order
          this.cartService.clearCart();
          
          // Show success message
          this.toastr.success(
            `Order #${response.result.orderNumber} placed successfully!`,
            'Order Confirmed'
          );
          
          // Navigate to order confirmation page
          this.router.navigate(['/orders', response.result.id]);
        } else {
          this.orderError = response.message || 'Failed to place order';
          this.toastr.error(this.orderError, 'Order Failed');
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.orderError = err.error?.message || 'Failed to place order. Please try again.';
        this.toastr.error(this.orderError, 'Order Failed');
      }
    });
  }
}