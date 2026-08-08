import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, UserSummary } from '../models/api-models';
import { API_ENDPOINTS } from '../config/api-endpoints';

const ACCESS_TOKEN_KEY = 'bts.accessToken';
const REFRESH_TOKEN_KEY = 'bts.refreshToken';
const USER_KEY = 'bts.user';

/**
 * Holds auth state in signals so components/guards/interceptors all read a single
 * source of truth reactively. Tokens are persisted to localStorage so a page reload
 * doesn't force a re-login; see SECURITY.md for the httpOnly-cookie hardening this
 * trades off against for an MVP SPA.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _accessToken = signal<string | null>(localStorage.getItem(ACCESS_TOKEN_KEY));
  private readonly _refreshToken = signal<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY));
  private readonly _currentUser = signal<UserSummary | null>(this.readStoredUser());

  readonly accessToken = this._accessToken.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._accessToken() !== null);
  readonly isAdmin = computed(() => this._currentUser()?.role === 'Admin');

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {}

  login(username: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}${API_ENDPOINTS.auth.login}`, { username, password })
      .pipe(tap((response) => this.setSession(response)));
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this._refreshToken();
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}${API_ENDPOINTS.auth.refresh}`, { refreshToken })
      .pipe(tap((response) => this.setSession(response)));
  }

  logout(): void {
    const refreshToken = this._refreshToken();
    const accessToken = this._accessToken();

    if (refreshToken && accessToken) {
      this.http.post(`${environment.apiBaseUrl}${API_ENDPOINTS.auth.logout}`, { refreshToken }, {
        headers: { Authorization: `Bearer ${accessToken}` }
      }).subscribe({ error: () => void 0 });
    }

    this.clearSession();
    this.router.navigate(['/login']);
  }

  getRefreshTokenValue(): string | null {
    return this._refreshToken();
  }

  private setSession(response: AuthResponse): void {
    this._accessToken.set(response.accessToken);
    this._refreshToken.set(response.refreshToken);
    this._currentUser.set(response.user);

    localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
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
