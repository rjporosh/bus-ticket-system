import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule],
  template: `
    <header class="client-header">
      <div class="header-content">
        <a routerLink="/" class="brand">🚌 BusTicketing</a>
        <nav class="main-nav">
          <a routerLink="/home" routerLinkActive="active" class="nav-link">Home</a>
          <a routerLink="/search" routerLinkActive="active" class="nav-link">Search Trips</a>
          <a routerLink="/my-tickets" routerLinkActive="active" class="nav-link">My Tickets</a>
        </nav>
        <div class="header-actions">
          <button mat-button routerLink="/login" *ngIf="!isLoggedIn()">Login</button>
          <button mat-raised-button color="primary" routerLink="/search" *ngIf="!isLoggedIn()">Book Now</button>
        </div>
      </div>
    </header>
    <main class="main-content">
      <router-outlet></router-outlet>
    </main>
    <footer class="client-footer">
      <div class="footer-content">
        <p>© 2025 BusTicketing. All rights reserved.</p>
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
  isLoggedIn() { return false; }
}