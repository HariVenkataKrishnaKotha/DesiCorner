import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '@core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { RegisterRequest } from '@core/models/auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './register.html',
  styleUrls: ['./register.scss']
})
export class Register implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  registerForm!: FormGroup;
  loading = false;
  hidePassword = true;
  hideConfirmPassword = true;

  dietaryOptions = [
    { value: 'Veg', label: 'Vegetarian' },
    { value: 'Non-Veg', label: 'Non-Vegetarian' },
    { value: 'Vegan', label: 'Vegan' }
  ];

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{9,14}$/)]],
      password: ['', [
        Validators.required, 
        Validators.minLength(8),
        this.passwordStrengthValidator
      ]],
      confirmPassword: ['', [Validators.required]],
      dietaryPreference: ['Veg', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  // Custom validator for password strength
  passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasDigit = /[0-9]/.test(value);

    const valid = hasUpperCase && hasLowerCase && hasDigit;
    
    if (!valid) {
      return { 
        passwordStrength: {
          hasUpperCase,
          hasLowerCase,
          hasDigit
        }
      };
    }
    return null;
  }

  // Cross-field validator for password match
  passwordMatchValidator(form: FormGroup): ValidationErrors | null {
    const password = form.get('password')?.value;
    const confirmPassword = form.get('confirmPassword')?.value;
    
    if (password && confirmPassword && password !== confirmPassword) {
      return { passwordMismatch: true };
    }
    return null;
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      Object.keys(this.registerForm.controls).forEach(key => {
        this.registerForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.loading = true;

    const request: RegisterRequest = {
      email: this.registerForm.value.email,
      password: this.registerForm.value.password,
      confirmPassword: this.registerForm.value.confirmPassword,
      phoneNumber: this.registerForm.value.phoneNumber,
      dietaryPreference: this.registerForm.value.dietaryPreference
    };

    this.authService.register(request).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess) {
          this.toastr.success(
            'Registration successful! Please verify your email address.',
            'Success'
          );
          // Navigate to verify-otp page with phone number
          this.router.navigate(['/auth/verify-otp'], {
            queryParams: {
              identifier: this.registerForm.value.email,
              purpose: 'Registration'
            }
          });
        } else {
          this.toastr.error(response.message || 'Registration failed', 'Error');
        }
      },
      error: (error) => {
        this.loading = false;
        const message = error.error?.message || error.message || 'Registration failed. Please try again.';
        this.toastr.error(message, 'Error');
      }
    });
  }

  // Helper to check if a field has error
  hasError(field: string, error: string): boolean {
    const control = this.registerForm.get(field);
    return control ? control.hasError(error) && control.touched : false;
  }

  // Check if passwords match (for template)
  get passwordMismatch(): boolean {
    return this.registerForm.hasError('passwordMismatch') && 
           this.registerForm.get('confirmPassword')?.touched === true;
  }
}