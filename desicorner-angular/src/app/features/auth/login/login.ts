import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '@core/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './login.html',
  styleUrls: ['./login.scss']
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  loginForm!: FormGroup;
  loading = false;
  hidePassword = true;

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false]
    });
  }

  onSubmit(): void {
  if (this.loginForm.invalid) {
    Object.keys(this.loginForm.controls).forEach(key => {
      this.loginForm.get(key)?.markAsTouched();
    });
    return;
  }

  this.loading = true;

  const loginRequest = {
    email: this.loginForm.value.email,
    password: this.loginForm.value.password,
    rememberMe: this.loginForm.value.rememberMe || false
  };

  console.log('Attempting login with:', loginRequest.email); // Debug log

  this.authService.login(loginRequest).subscribe({
    next: (response) => {
      console.log('Login response:', response); // Debug log
      
      if (response.isSuccess) {
        this.toastr.success('Login successful!', 'Welcome');
        this.authService.loadUserProfile();
        
        // Redirect to home or return URL
        const returnUrl = sessionStorage.getItem('returnUrl') || '/';
        sessionStorage.removeItem('returnUrl');
        this.router.navigate([returnUrl]);
      } else {
        this.toastr.error(response.message || 'Login failed. Please check your credentials.', 'Error');
      }
      this.loading = false;
    },
    error: (error) => {
      console.error('Login error:', error); // Debug log
      // Error interceptor will show the toast
      this.loading = false;
    }
  });
}
}