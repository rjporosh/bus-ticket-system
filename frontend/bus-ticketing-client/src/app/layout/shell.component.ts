import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../core/services/auth.service';
import { LanguageSwitcherComponent } from '../features/shared/language-switcher/language-switcher.component';
import { TranslatePipe } from '../core/pipes/translate.pipe';
import { OnlineStatusService } from '../core/services/online-status.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule, LanguageSwitcherComponent, TranslatePipe],
  template: `
    <header class="client-header">
      <div class="header-content">
        <a routerLink="/" class="brand">🚌 {{ 'app.title' | translate }}</a>
        <nav class="main-nav">
          <a routerLink="/home" routerLinkActive="active" class="nav-link">{{ 'app.home' | translate }}</a>
          <a routerLink="/search" routerLinkActive="active" class="nav-link">{{ 'app.search' | translate }}</a>
          @if (auth.isAuthenticated()) {
            <a routerLink="/my-tickets" routerLinkActive="active" class="nav-link">{{ 'app.myTickets' | translate }}</a>
          }
        </nav>
        <div class="header-actions">
          <span class="connection-status" [class.offline]="!onlineStatus.isOnline()">
            {{ onlineStatus.isOnline() ? ('app.online' | translate) : ('app.offline' | translate) }}
          </span>
          <app-language-switcher />
          @if (auth.isAuthenticated()) {
            <span class="user-name">{{ auth.currentUser()?.fullName }}</span>
            <button mat-button (click)="logout()">{{ 'app.logout' | translate }}</button>
          } @else {
            <button mat-button routerLink="/login">{{ 'app.login' | translate }}</button>
            <button mat-raised-button color="primary" routerLink="/register">{{ 'app.register' | translate }}</button>
          }
        </div>
      </div>
    </header>
    <main class="main-content">
      <router-outlet></router-outlet>
    </main>
    <footer class="client-footer">
      <div class="footer-content">
        <p>© 2025 BusTicketing. {{ 'app.allRightsReserved' | translate }}</p>
      </div>
    </footer>
  `,
  styles: [`
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    .client-header { background: linear-gradient(135deg, #1a73e8 0%, #0d47a1 100%); color: white; padding: 1rem 0; box-shadow: 0 2px 8px rgba(0,0,0,0.15); }
    .header-content { max-width: 1200px; margin: 0 auto; padding: 0 2rem; display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
    .brand { color: white; font-size: 1.5rem; font-weight: 700; text-decoration: none; }
    .main-nav { display: flex; gap: 0.5rem; }
    .nav-link { color: rgba(255,255,255,0.9); text-decoration: none; padding: 0.5rem 1rem; border-radius: 4px; font-weight: 500; transition: background 0.2s; }
    .nav-link:hover, .nav-link.active { background: rgba(255,255,255,0.15); color: white; }
    .header-actions { display: flex; align-items: center; gap: 0.75rem; }
    .connection-status { font-size: 0.75rem; padding: 2px 8px; border-radius: 12px; background: rgba(255,255,255,0.2); }
    .connection-status.offline { background: rgba(255,152,0,0.3); }
    .user-name { font-size: 0.875rem; opacity: 0.9; }
    .main-content { flex: 1; padding: 2rem 0; background: #f5f7fa; }
    .client-footer { background: #1a1a2e; color: #aaa; padding: 1.5rem 0; text-align: center; }
    .footer-content { max-width: 1200px; margin: 0 auto; padding: 0 2rem; }
    @media (max-width: 768px) {
      .header-content { flex-direction: column; }
      .main-nav { flex-wrap: wrap; justify-content: center; }
    }
  `]
})
export class ShellComponent {
  protected readonly auth = inject(AuthService);
  protected readonly onlineStatus = inject(OnlineStatusService);

  logout(): void {
    this.auth.logout();
  }
}
