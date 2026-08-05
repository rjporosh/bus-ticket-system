import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, UserSummary } from '../models/api-models';

const ACCESS_TOKEN_KEY = 'bts.client.accessToken';
const REFRESH_TOKEN_KEY = 'bts.client.refreshToken';
const USER_KEY = 'bts.client.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _accessToken = signal<string | null>(localStorage.getItem(ACCESS_TOKEN_KEY));
  private readonly _refreshToken = signal<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY));
  private readonly _currentUser = signal<UserSummary | null>(this.readStoredUser());

  readonly accessToken = this._accessToken.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._accessToken() !== null);

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(tap((response) => this.setSession(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/register`, request)
      .pipe(tap((response) => this.setSession(response)));
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this._refreshToken();
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, { refreshToken })
      .pipe(tap((response) => this.setSession(response)));
  }

  logout(): void {
    const refreshToken = this._refreshToken();
    this.clearSession();
    this.router.navigate(['/home']);

    if (refreshToken) {
      this.http.post(`${environment.apiBaseUrl}/auth/logout`, { refreshToken }).subscribe({ error: () => void 0 });
    }
  }

  getRefreshTokenValue(): string | null {
    return this._refreshToken();
  }

  private setSession(response: AuthResponse): void {
    this._accessToken.set(response.accessToken);
    this._refreshToken.set(response.refreshToken);
    this._currentUser.set({
      id: response.user.id,
      username: response.user.username,
      email: response.user.email,
      fullName: response.user.fullName,
      role: response.user.role,
    });

    localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify({
      id: response.user.id,
      username: response.user.username,
      email: response.user.email,
      fullName: response.user.fullName,
      role: response.user.role,
    }));
  }

  private clearSession(): void {
    this._accessToken.set(null);
    this._refreshToken.set(null);
    this._currentUser.set(null);

    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  private readStoredUser(): UserSummary | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as UserSummary;
    } catch {
      return null;
    }
  }
}