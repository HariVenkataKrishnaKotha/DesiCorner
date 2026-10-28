import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '@core/services/auth.service';
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
  private router = inject(Router);
  private toastr = inject(ToastrService);

  error = false;

  async ngOnInit(): Promise<void> {
    try {
      const success = await this.authService.handlePkceCallback();

      if (success) {
        this.toastr.success('Login successful!', 'Welcome');

        // The cart is loaded by CartEffects after the guest cart has been merged (signInCompleted)

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
