import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '@core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { interval, Subscription } from 'rxjs';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './verify-otp.html',
  styleUrls: ['./verify-otp.scss']
})
export class VerifyOtp implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);

  otpForm!: FormGroup;
  loading = false;
  resending = false;
  
  // Timer for OTP expiry (10 minutes = 600 seconds)
  timeRemaining = 600;
  timerSubscription?: Subscription;
  
  // Query params
  identifier = '';
  purpose = 'Registration';
  
  // Resend cooldown (60 seconds)
  resendCooldown = 0;
  resendSubscription?: Subscription;

  ngOnInit(): void {
    // Get query params
    this.route.queryParams.subscribe(params => {
      this.identifier = params['identifier'] || '';
      this.purpose = params['purpose'] || 'Registration';
      
      if (!this.identifier) {
        this.toastr.error('No email address provided', 'Error');
        this.router.navigate(['/auth/register']);
        return;
      }
    });

    // Initialize form
    this.otpForm = this.fb.group({
      otp: ['', [
        Validators.required, 
        Validators.minLength(6), 
        Validators.maxLength(6),
        Validators.pattern(/^\d{6}$/)
      ]]
    });

    // Start countdown timer
    this.startTimer();
  }

  ngOnDestroy(): void {
    this.timerSubscription?.unsubscribe();
    this.resendSubscription?.unsubscribe();
  }

  private startTimer(): void {
    this.timeRemaining = 600; // 10 minutes
    this.timerSubscription?.unsubscribe();
    
    this.timerSubscription = interval(1000)
      .pipe(take(600))
      .subscribe(() => {
        this.timeRemaining--;
        if (this.timeRemaining <= 0) {
          this.timerSubscription?.unsubscribe();
        }
      });
  }

  private startResendCooldown(): void {
    this.resendCooldown = 60; // 60 seconds cooldown
    this.resendSubscription?.unsubscribe();
    
    this.resendSubscription = interval(1000)
      .pipe(take(60))
      .subscribe(() => {
        this.resendCooldown--;
      });
  }

  get formattedTime(): string {
    const minutes = Math.floor(this.timeRemaining / 60);
    const seconds = this.timeRemaining % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  get isExpired(): boolean {
    return this.timeRemaining <= 0;
  }

  get maskedIdentifier(): string {
  if (!this.identifier) return '';
  // Mask email: user@example.com -> us****@example.com
  const atIndex = this.identifier.indexOf('@');
  if (atIndex > 2) {
    const localPart = this.identifier.slice(0, atIndex);
    const domain = this.identifier.slice(atIndex);
    const visibleStart = localPart.slice(0, 2);
    return `${visibleStart}****${domain}`;
  }
  return this.identifier;
}

  onSubmit(): void {
    if (this.otpForm.invalid) {
      this.otpForm.get('otp')?.markAsTouched();
      return;
    }

    if (this.isExpired) {
      this.toastr.error('OTP has expired. Please request a new one.', 'Expired');
      return;
    }

    this.loading = true;

    this.authService.verifyOtp({
      identifier: this.identifier,
      otp: this.otpForm.value.otp
    }).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess) {
          this.toastr.success(
            'Email address verified successfully! Please login to continue.',
            'Verified'
          );
          this.router.navigate(['/auth/login']);
        } else {
          this.toastr.error(response.message || 'Invalid OTP', 'Error');
        }
      },
      error: (error) => {
        this.loading = false;
        const message = error.error?.message || error.message || 'Verification failed';
        this.toastr.error(message, 'Error');
      }
    });
  }

  resendOtp(): void {
    if (this.resendCooldown > 0 || this.resending) {
      return;
    }

    this.resending = true;

    this.authService.sendOtp({
      email: this.identifier,
      purpose: this.purpose,
      deliveryMethod: 'Email'
    }).subscribe({
      next: (response) => {
        this.resending = false;
        if (response.isSuccess) {
          this.toastr.success('New OTP sent successfully!', 'OTP Sent');
          this.startTimer(); // Reset the main timer
          this.startResendCooldown(); // Start cooldown for resend button
          this.otpForm.reset(); // Clear the form
        } else {
          this.toastr.error(response.message || 'Failed to resend OTP', 'Error');
        }
      },
      error: (error) => {
        this.resending = false;
        const message = error.error?.message || 'Failed to resend OTP';
        this.toastr.error(message, 'Error');
      }
    });
  }

  // Only allow numeric input
  onOtpInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    input.value = input.value.replace(/\D/g, '').slice(0, 6);
    this.otpForm.get('otp')?.setValue(input.value);
  }
}