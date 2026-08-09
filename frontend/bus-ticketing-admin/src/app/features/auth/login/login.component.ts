import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { ProblemDetails } from '../../../core/models/api-models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="login-screen">
      <mat-card class="login-card card-surface">
        <div class="login-card__header">
          <div class="login-card__badge">
            <mat-icon>directions_bus</mat-icon>
          </div>
          <h1>Bus Ticketing System</h1>
          <p class="mono">Booth Staff &amp; Admin Login</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Username</mat-label>
            <input matInput formControlName="username" autocomplete="username" />
            @if (form.controls.username.hasError('required') && form.controls.username.touched) {
              <mat-error>Username is required.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Password</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="current-password" />
            @if (form.controls.password.hasError('required') && form.controls.password.touched) {
              <mat-error>Password is required.</mat-error>
            }
          </mat-form-field>

          @if (errorMessage()) {
            <p class="login-card__error">{{ errorMessage() }}</p>
          }

          <button mat-flat-button color="primary" type="submit" class="full-width login-card__submit" [disabled]="submitting()">
            @if (submitting()) {
              <mat-spinner diameter="20" />
            } @else {
              Sign in
            }
          </button>
        </form>

        <div class="login-card__hint">
          <span class="mono">Example users:</span>
          dhaka_staff_1 · ctg_staff_1 · admin
        </div>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .login-screen {
        min-height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-ink-900);
        background-image: radial-gradient(circle at 20% 20%, var(--color-ink-700), var(--color-ink-900) 60%);
        padding: 24px;
      }

      .login-card {
        width: 100%;
        max-width: 380px;
        padding: 32px;
      }

      .login-card__header {
        text-align: center;
        margin-bottom: 24px;
      }

      .login-card__badge {
        width: 48px;
        height: 48px;
        border-radius: 50%;
        background: var(--color-available-bg);
        display: flex;
        align-items: center;
        justify-content: center;
        margin: 0 auto 12px;

        mat-icon {
          color: var(--color-accent-ink);
        }
      }

      .login-card__header p {
        color: var(--color-text-muted);
        font-size: 0.75rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .full-width {
        width: 100%;
      }

      .login-card__submit {
        height: 44px;
        margin-top: 8px;
      }

      .login-card__error {
        color: var(--color-sold);
        font-size: 0.85rem;
        margin: -8px 0 12px;
      }

      .login-card__hint {
        margin-top: 20px;
        text-align: center;
        font-size: 0.75rem;
        color: var(--color-text-muted);
      }
    `,
  ],
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);

    const { username, password } = this.form.getRawValue();
    this.auth.login(username, password).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        const problem = error.error as ProblemDetails | undefined;
        this.errorMessage.set(problem?.title ?? problem?.detail ?? 'Invalid username or password.');
      },
    });
  }
}
