import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterRequest } from '../../../core/models/api-models';
import { ProblemDetails } from '../../../core/models/api-models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="register-screen">
      <mat-card class="register-card card-surface">
        <div class="register-card__header">
          <div class="register-card__badge">
            <mat-icon>person_add</mat-icon>
          </div>
          <h1>Create Account</h1>
          <p class="mono">Join BusTicketing to book and manage your trips</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Full Name</mat-label>
            <input matInput formControlName="fullName" autocomplete="name" />
            @if (form.controls.fullName.hasError('required') && form.controls.fullName.touched) {
              <mat-error>Full name is required.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Username</mat-label>
            <input matInput formControlName="username" autocomplete="username" />
            @if (form.controls.username.hasError('required') && form.controls.username.touched) {
              <mat-error>Username is required.</mat-error>
            }
            @if (form.controls.username.hasError('minlength') && form.controls.username.touched) {
              <mat-error>Username must be at least 3 characters.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Email</mat-label>
            <input matInput formControlName="email" type="email" autocomplete="email" />
            @if (form.controls.email.hasError('required') && form.controls.email.touched) {
              <mat-error>Email is required.</mat-error>
            }
            @if (form.controls.email.hasError('email') && form.controls.email.touched) {
              <mat-error>Enter a valid email address.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Phone (optional)</mat-label>
            <input matInput formControlName="phoneNumber" autocomplete="tel" placeholder="01XXXXXXXXX" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Password</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="new-password" />
            @if (form.controls.password.hasError('required') && form.controls.password.touched) {
              <mat-error>Password is required.</mat-error>
            }
            @if (form.controls.password.hasError('minlength') && form.controls.password.touched) {
              <mat-error>Password must be at least 8 characters.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Confirm Password</mat-label>
            <input matInput type="password" formControlName="confirmPassword" autocomplete="new-password" />
            @if (form.controls.confirmPassword.hasError('required') && form.controls.confirmPassword.touched) {
              <mat-error>Please confirm your password.</mat-error>
            }
            @if (form.controls.confirmPassword.hasError('mismatch') && form.controls.confirmPassword.touched) {
              <mat-error>Passwords do not match.</mat-error>
            }
          </mat-form-field>

          @if (errorMessage()) {
            <p class="register-card__error">{{ errorMessage() }}</p>
          }

          <button mat-flat-button color="primary" type="submit" class="full-width register-card__submit" [disabled]="submitting()">
            @if (submitting()) {
              <mat-spinner diameter="20" />
            } @else {
              Create Account
            }
          </button>
        </form>

        <div class="register-card__footer">
          <span>Already have an account?</span>
          <a routerLink="/login">Sign in</a>
        </div>
      </mat-card>
    </div>
  `,
  styles: [`
    .register-screen {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #1a73e8 0%, #0d47a1 100%);
      padding: 24px;
    }
    .register-card {
      width: 100%;
      max-width: 440px;
      padding: 32px;
    }
    .register-card__header { text-align: center; margin-bottom: 24px; }
    .register-card__badge {
      width: 48px; height: 48px; border-radius: 50%;
      background: #e3f2fd;
      display: flex; align-items: center; justify-content: center;
      margin: 0 auto 12px;
      mat-icon { color: #1a73e8; }
    }
    .register-card__header h1 { margin: 0 0 8px; font-size: 1.5rem; color: #333; }
    .register-card__header p { margin: 0; color: #666; font-size: 0.8rem; }
    .full-width { width: 100%; }
    .register-card__submit { height: 44px; margin-top: 8px; }
    .register-card__error { color: #d32f2f; font-size: 0.85rem; margin: -8px 0 12px; }
    .register-card__footer { margin-top: 20px; text-align: center; font-size: 0.85rem; color: #666; }
    .register-card__footer a { color: #1a73e8; text-decoration: none; font-weight: 600; margin-left: 4px; }
    .register-card__footer a:hover { text-decoration: underline; }
  `],
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group(
    {
      fullName: ['', Validators.required],
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: this.passwordMatchValidator },
  );

  private passwordMatchValidator(form: { get: (key: string) => { value: string; hasError: (key: string) => boolean } | null }): { mismatch: boolean } | null {
    const password = form.get('password')?.value;
    const confirm = form.get('confirmPassword')?.value;
    return password === confirm ? null : { mismatch: true };
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);

    const { fullName, username, email, phoneNumber, password } = this.form.getRawValue();
    const request: RegisterRequest = { fullName, username, email, phoneNumber: phoneNumber || undefined, password };

    this.auth.register(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/my-tickets']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        const problem = error.error as ProblemDetails | undefined;
        this.errorMessage.set(problem?.detail ?? problem?.title ?? 'Registration failed. Please try again.');
      },
    });
  }
}