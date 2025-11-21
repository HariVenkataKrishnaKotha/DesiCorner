import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CartService } from '@core/services/cart.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [
  CommonModule,
  RouterModule,
  FormsModule,
  MatCardModule,
  MatButtonModule,
  MatIconModule,
  MatFormFieldModule,
  MatInputModule,
  MatProgressSpinnerModule
],
  templateUrl: './cart.html',
  styleUrls: ['./cart.scss']
})
export class CartComponent implements OnInit, OnDestroy {
  cartService = inject(CartService);
  
  private readonly COUPON_DRAFT_KEY = 'desicorner_coupon_draft';
  
  couponCode = '';
  couponLoading = false;
  couponError = '';

  ngOnInit(): void {
    // Load draft coupon code from localStorage
    const savedCoupon = localStorage.getItem(this.COUPON_DRAFT_KEY);
    if (savedCoupon) {
      this.couponCode = savedCoupon;
    }
  }

  ngOnDestroy(): void {
    // Save draft coupon code when leaving the page
    if (this.couponCode.trim()) {
      localStorage.setItem(this.COUPON_DRAFT_KEY, this.couponCode.trim());
    } else {
      localStorage.removeItem(this.COUPON_DRAFT_KEY);
    }
  }

  applyCoupon(): void {
    if (!this.couponCode.trim()) return;
    
    this.couponLoading = true;
    this.couponError = '';
    
    this.cartService.applyCoupon(this.couponCode.trim()).subscribe({
      next: (response) => {
        this.couponLoading = false;
        if (response.isSuccess) {
          this.couponCode = '';
          // Clear draft since coupon was applied
          localStorage.removeItem(this.COUPON_DRAFT_KEY);
        } else {
          this.couponError = response.message || 'Failed to apply coupon';
        }
      },
      error: (err) => {
        this.couponLoading = false;
        this.couponError = err.error?.message || 'Failed to apply coupon';
      }
    });
  }

  removeCoupon(): void {
    this.couponLoading = true;
    this.couponError = '';
    
    this.cartService.removeCoupon().subscribe({
      next: () => {
        this.couponLoading = false;
      },
      error: (err) => {
        this.couponLoading = false;
        this.couponError = err.error?.message || 'Failed to remove coupon';
      }
    });
  }
}