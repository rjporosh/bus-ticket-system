import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../../core/services/api.service';
import { LoginRequest, AuthResponse } from '../../../core/models/api-models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <h1>Client Login</h1>
          <p>Sign in to view your bookings</p>
        </div>
        <form [formGroup]="loginForm" (ngSubmit)="onLogin()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Email</mat-label>
            <input matInput formControlName="email" type="email" placeholder="your@email.com">
            <mat-error *ngIf="loginForm.get('email')?.hasError('required')">Email is required</mat-error>
            <mat-error *ngIf="loginForm.get('email')?.hasError('email')">Invalid email format</mat-error>
          </mat-form-field>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Password</mat-label>
            <input matInput formControlName="password" type="password">
            <mat-error *ngIf="loginForm.get('password')?.hasError('required')">Password is required</mat-error>
          </mat-form-field>
          <button mat-raised-button color="primary" type="submit" class="full-width" [disabled]="loginForm.invalid || loading()">
            {{ loading() ? 'Signing in...' : 'Sign In' }}
          </button>
        </form>
        <div class="login-footer">
          <p>Don't have an account? <a routerLink="/register">Register</a></p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-container { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: linear-gradient(135deg, #1a73e8 0%, #0d47a1 100%); padding: 2rem; }
    .login-card { background: white; padding: 2.5rem; border-radius: 8px; box-shadow: 0 8px 24px rgba(0,0,0,0.15); width: 100%; max-width: 400px; }
    .login-header { text-align: center; margin-bottom: 2rem; }
    .login-header h1 { color: #333; margin: 0 0 0.5rem; }
    .login-header p { color: #666; margin: 0; }
    .full-width { width: 100%; margin-bottom: 1rem; }
    .login-footer { text-align: center; margin-top: 1.5rem; color: #666; }
    .login-footer a { color: #1a73e8; text-decoration: none; font-weight: 600; }
    .login-footer a:hover { text-decoration: underline; }
  `]
})
export class LoginComponent {
  loginForm: FormGroup;
  loading = signal(false);

  constructor(private fb: FormBuilder, private api: ApiService, private router: Router) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  onLogin(): void {
    if (this.loginForm.invalid) return;
    this.loading.set(true);
    const request: LoginRequest = this.loginForm.value;
    // TODO: Call api.post<LoginRequest, AuthResponse>('/auth/login', request)
    setTimeout(() => {
      this.loading.set(false);
      alert('Login successful! (Mock)');
      this.router.navigate(['/my-tickets']);
    }, 1000);
  }
}