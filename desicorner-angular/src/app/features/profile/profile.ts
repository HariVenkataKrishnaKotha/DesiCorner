import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';

import { AuthService } from '../../core/services/auth.service';
import { ProfileService } from '../../core/services/profile.service';
import { UserProfile, DeliveryAddress, AddAddressRequest, ChangePasswordRequest } from '../../core/models/profile.models';
@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatChipsModule,
    MatSnackBarModule,
    MatDialogModule,
    MatListModule
  ],
  templateUrl: './profile.html',
  styleUrls: ['./profile.scss']
})
export class ProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private profileService = inject(ProfileService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private snackBar = inject(MatSnackBar);

  // State
  profile: UserProfile | null = null;
  loading = true;
  error = '';

  // Address form
  showAddressForm = false;
  addressForm!: FormGroup;
  savingAddress = false;
  states: string[] = [];
  addressLabels: string[] = [];

  // Password form
  passwordForm!: FormGroup;
  changingPassword = false;
  hideCurrentPassword = true;
  hideNewPassword = true;
  hideConfirmPassword = true;

  ngOnInit(): void {
    // Initialize forms
    this.initForms();
    
    // Load helper data
    this.states = this.profileService.getStates();
    this.addressLabels = this.profileService.getAddressLabels();

    // Check authentication and load profile
    this.authService.authState$.subscribe(state => {
      if (!state.isAuthenticated && !state.loading) {
        this.router.navigate(['/auth/login'], { queryParams: { returnUrl: '/profile' } });
      } else if (state.isAuthenticated && state.profile) {
        this.profile = state.profile;
        this.loading = false;
      } else if (state.isAuthenticated) {
        this.loadProfile();
      }
    });
  }

  private initForms(): void {
    // Address form
    this.addressForm = this.fb.group({
      label: ['Home', Validators.required],
      addressLine1: ['', [Validators.required, Validators.maxLength(200)]],
      addressLine2: [''],
      city: ['', [Validators.required, Validators.maxLength(100)]],
      state: ['', Validators.required],
      zipCode: ['', [Validators.required, Validators.pattern(/^\d{5}(-\d{4})?$/)]],
      isDefault: [false]
    });

    // Password form
    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  private passwordMatchValidator(form: FormGroup): { [key: string]: boolean } | null {
    const newPassword = form.get('newPassword')?.value;
    const confirmPassword = form.get('confirmNewPassword')?.value;
    
    if (newPassword && confirmPassword && newPassword !== confirmPassword) {
      return { passwordMismatch: true };
    }
    return null;
  }

  loadProfile(): void {
    this.loading = true;
    this.error = '';

    this.profileService.getProfile().subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess && response.result) {
          this.profile = response.result;
        } else {
          this.error = response.message || 'Failed to load profile';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to load profile';
      }
    });
  }

  // Address Management
  toggleAddressForm(): void {
    this.showAddressForm = !this.showAddressForm;
    if (!this.showAddressForm) {
      this.addressForm.reset({ label: 'Home', isDefault: false });
    }
  }

  saveAddress(): void {
    if (this.addressForm.invalid) {
      this.addressForm.markAllAsTouched();
      return;
    }

    this.savingAddress = true;
    const addressData: AddAddressRequest = this.addressForm.value;

    this.profileService.addAddress(addressData).subscribe({
      next: (response) => {
        this.savingAddress = false;
        if (response.isSuccess) {
          this.snackBar.open('Address added successfully!', 'Close', { duration: 3000 });
          this.toggleAddressForm();
          // Explicitly reload profile to get updated addresses
          this.profileService.getProfile().subscribe({
            next: (profileResponse) => {
              if (profileResponse.isSuccess && profileResponse.result) {
                this.profile = profileResponse.result;
              }
            }
          });
        } else {
          this.snackBar.open(response.message || 'Failed to add address', 'Close', { duration: 3000 });
        }
      },
      error: (err) => {
        this.savingAddress = false;
        this.snackBar.open(err.error?.message || 'Failed to add address', 'Close', { duration: 3000 });
      }
    });
  }

  // Password Change
  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.changingPassword = true;
    const passwordData: ChangePasswordRequest = this.passwordForm.value;

    this.profileService.changePassword(passwordData).subscribe({
      next: (response) => {
        this.changingPassword = false;
        if (response.isSuccess) {
          this.snackBar.open('Password changed successfully!', 'Close', { duration: 3000 });
          this.passwordForm.reset();
        } else {
          this.snackBar.open(response.message || 'Failed to change password', 'Close', { duration: 3000 });
        }
      },
      error: (err) => {
        this.changingPassword = false;
        this.snackBar.open(err.error?.message || 'Failed to change password', 'Close', { duration: 3000 });
      }
    });
  }

  // Helper methods
  getDefaultAddress(): DeliveryAddress | undefined {
    return this.profile?.addresses?.find(a => a.isDefault);
  }

  formatAddress(address: DeliveryAddress): string {
    let formatted = address.addressLine1;
    if (address.addressLine2) {
      formatted += `, ${address.addressLine2}`;
    }
    formatted += `, ${address.city}, ${address.state} ${address.zipCode}`;
    return formatted;
  }
}