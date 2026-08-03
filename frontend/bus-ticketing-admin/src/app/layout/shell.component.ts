import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from '../core/services/auth.service';

interface NavItem {
  label: string;
  icon: string;
  path: string;
  adminOnly?: boolean;
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', icon: 'dashboard', path: '/dashboard' },
  { label: 'Ticketing', icon: 'confirmation_number', path: '/booking' },
  { label: 'Schedules', icon: 'event', path: '/schedules' },
  { label: 'Buses', icon: 'directions_bus', path: '/buses' },
  { label: 'Routes', icon: 'alt_route', path: '/routes' },
  { label: 'Stations', icon: 'location_on', path: '/stations' },
  { label: 'Users', icon: 'group', path: '/users', adminOnly: true },
  { label: 'Roles', icon: 'admin_panel_settings', path: '/roles', adminOnly: true },
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatIconModule, MatButtonModule, MatMenuModule, MatToolbarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="sidebar__brand">
          <mat-icon>directions_bus</mat-icon>
          <div>
            <div class="sidebar__brand-title">Bus Ticketing</div>
            <div class="sidebar__brand-subtitle">Dispatch Console</div>
          </div>
        </div>

        <nav class="sidebar__nav">
          @for (item of visibleNavItems(); track item.path) {
            <a [routerLink]="item.path" routerLinkActive="active" class="sidebar__link">
              <mat-icon>{{ item.icon }}</mat-icon>
              <span>{{ item.label }}</span>
            </a>
          }
        </nav>

        <div class="sidebar__footer">
          <span class="mono">v1.0.0 · MVP</span>
        </div>
      </aside>

      <div class="shell__main">
        <mat-toolbar class="topbar">
          <span class="topbar__spacer"></span>
          <span class="board-chip board-chip--accent mono">{{ today() }}</span>

          <button mat-icon-button [matMenuTriggerFor]="userMenu" aria-label="Account menu">
            <mat-icon>account_circle</mat-icon>
          </button>
          <mat-menu #userMenu="matMenu">
            <div class="user-menu-header">
              <strong>{{ auth.currentUser()?.fullName }}</strong>
              <div class="mono">{{ auth.currentUser()?.role }}{{ auth.currentUser()?.boothName ? ' · ' + auth.currentUser()?.boothName : '' }}</div>
            </div>
            <button mat-menu-item (click)="auth.logout()">
              <mat-icon>logout</mat-icon>
              <span>Sign out</span>
            </button>
          </mat-menu>
        </mat-toolbar>

        <main class="shell__content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  protected readonly auth = inject(AuthService);
  protected readonly today = signal(
    new Date().toLocaleDateString('en-GB', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' }).toUpperCase(),
  );

  protected visibleNavItems(): NavItem[] {
    const isAdmin = this.auth.isAdmin();
    return NAV_ITEMS.filter((item) => !item.adminOnly || isAdmin);
  }
}
