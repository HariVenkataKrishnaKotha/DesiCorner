import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '@core/services/auth.service';
import { CartService } from '@core/services/cart.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-callback',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  templateUrl: './callback.html',
  styleUrl: './callback.scss',
})
export class Callback implements OnInit {
  private authService = inject(AuthService);
  private cartService = inject(CartService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  error = false;

  async ngOnInit(): Promise<void> {
    try {
      const success = await this.authService.handlePkceCallback();

      if (success) {
        this.toastr.success('Login successful!', 'Welcome');

        // Reload cart from server after login
        this.cartService.loadCart();

        // Redirect to return URL or home
        const returnUrl = sessionStorage.getItem('returnUrl') || '/';
        sessionStorage.removeItem('returnUrl');
        this.router.navigate([returnUrl]);
      } else {
        this.error = true;
        this.toastr.error('Authentication failed. Please try again.', 'Error');
        setTimeout(() => this.router.navigate(['/auth/login']), 3000);
      }
    } catch {
      this.error = true;
      this.toastr.error('Authentication failed. Please try again.', 'Error');
      setTimeout(() => this.router.navigate(['/auth/login']), 3000);
    }
  }
}
