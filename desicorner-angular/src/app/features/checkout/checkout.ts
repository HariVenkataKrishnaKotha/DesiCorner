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
import { ToastrService } from 'ngx-toastr';

import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { Cart } from '../../core/models/cart.models';

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
    MatProgressSpinnerModule
  ],
  templateUrl: './checkout.html',
  styleUrls: ['./checkout.scss']
})
export class CheckoutComponent implements OnInit {
  private cartService = inject(CartService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  cart: Cart = { items: [], subtotal: 0, tax: 0, deliveryFee: 0, discount: 0, total: 0 };
  checkoutForm!: FormGroup;
  isSubmitting = false;
  isAuthenticated = false;

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
      zipCode: ['', Validators.required],
      
      // Optional
      instructions: ['']
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
      return;
    }

    this.isSubmitting = true;

    // Prepare order data
    const formValue = this.checkoutForm.value;
    const orderData = {
      items: this.cart.items.map(item => ({
        productId: item.productId,
        quantity: item.quantity,
        price: item.price
      })),
      deliveryAddress: {
        fullName: formValue.fullName,
        street: formValue.street,
        apartment: formValue.apartment,
        city: formValue.city,
        state: formValue.state,
        zipCode: formValue.zipCode
      },
      contactInfo: !this.isAuthenticated ? {
        email: formValue.email,
        phone: formValue.phone
      } : null,
      instructions: formValue.instructions,
      couponCode: this.cart.couponCode,
      subtotal: this.cart.subtotal,
      tax: this.cart.tax,
      deliveryFee: this.cart.deliveryFee,
      discount: this.cart.discount,
      total: this.cart.total
    };

    // TODO: Call order API when implemented
    console.log('Order data:', orderData);
    
    setTimeout(() => {
      this.isSubmitting = false;
      this.toastr.success('Order placed successfully!', 'Success');
      this.cartService.clearCart();
      this.router.navigate(['/']);
    }, 1500);
  }
}