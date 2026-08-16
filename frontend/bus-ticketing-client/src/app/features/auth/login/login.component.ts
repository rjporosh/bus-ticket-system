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
import { LoginRequest, ProblemDetails } from '../../../core/models/api-models';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { TranslateService } from '../../../core/services/translate.service';

@Component({
  selector: 'app-login',
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
    TranslatePipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="login-screen">
      <mat-card class="login-card card-surface">
        <div class="login-card__header">
          <div class="login-card__badge">
            <mat-icon>directions_bus</mat-icon>
          </div>
          <h1>{{ 'app.loginTitle' | translate }}</h1>
          <p class="mono">{{ 'app.loginSubtitle' | translate }}</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'app.usernameOrEmail' | translate }}</mat-label>
            <input matInput formControlName="username" autocomplete="username" />
            @if (form.controls.username.hasError('required') && form.controls.username.touched) {
              <mat-error>{{ 'app.usernameRequired' | translate }}</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'app.password' | translate }}</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="current-password" />
            @if (form.controls.password.hasError('required') && form.controls.password.touched) {
              <mat-error>{{ 'app.passwordRequired' | translate }}</mat-error>
            }
          </mat-form-field>

          @if (errorMessage()) {
            <p class="login-card__error">{{ errorMessage() }}</p>
          }

          <button mat-flat-button color="primary" type="submit" class="full-width login-card__submit" [disabled]="submitting()">
            @if (submitting()) {
              <mat-spinner diameter="20" />
            } @else {
              {{ 'app.signIn' | translate }}
            }
          </button>
        </form>

        <div class="login-card__footer">
          <span>{{ 'app.dontHaveAccount' | translate }}</span>
          <a routerLink="/register">{{ 'app.registerNow' | translate }}</a>
        </div>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-screen {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #1a73e8 0%, #0d47a1 100%);
      padding: 24px;
    }
    .login-card {
      width: 100%;
      max-width: 400px;
      padding: 32px;
    }
    .login-card__header { text-align: center; margin-bottom: 24px; }
    .login-card__badge {
      width: 48px; height: 48px; border-radius: 50%;
      background: #e3f2fd;
      display: flex; align-items: center; justify-content: center;
      margin: 0 auto 12px;
      mat-icon { color: #1a73e8; }
    }
    .login-card__header h1 { margin: 0 0 8px; font-size: 1.5rem; color: #333; }
    .login-card__header p { margin: 0; color: #666; font-size: 0.8rem; }
    .full-width { width: 100%; }
    .login-card__submit { height: 44px; margin-top: 8px; }
    .login-card__error { color: #d32f2f; font-size: 0.85rem; margin: -8px 0 12px; }
    .login-card__footer { margin-top: 20px; text-align: center; font-size: 0.85rem; color: #666; }
    .login-card__footer a { color: #1a73e8; text-decoration: none; font-weight: 600; margin-left: 4px; }
    .login-card__footer a:hover { text-decoration: underline; }
  `],
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

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
    const request: LoginRequest = { username, password };

    this.auth.login(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/my-tickets']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        const problem = error.error as ProblemDetails | undefined;
        this.errorMessage.set(problem?.title ?? problem?.detail ?? this.translate.getSync('app.invalidCredentials'));
      },
    });
  }
}