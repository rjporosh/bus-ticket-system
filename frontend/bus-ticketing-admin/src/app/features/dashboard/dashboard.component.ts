import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardService } from '../../core/services/feature-services';
import { BusSeatStatusDto, DashboardSummaryDto, RouteSalesDto } from '../../core/models/api-models';

interface SummaryTile {
  label: string;
  value: string;
  icon: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MatCardModule, MatIconModule, MatTableModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <h1>Today's Overview</h1>
      <p class="mono subtitle">{{ todayLabel() }}</p>

      @if (loading()) {
        <div class="loading-row"><mat-spinner diameter="28" /></div>
      } @else {
        <div class="tiles">
          @for (tile of tiles(); track tile.label) {
            <mat-card class="card-surface tile">
              <mat-icon class="tile__icon">{{ tile.icon }}</mat-icon>
              <div>
                <div class="tile__value mono">{{ tile.value }}</div>
                <div class="tile__label">{{ tile.label }}</div>
              </div>
            </mat-card>
          }
        </div>

        <div class="two-col">
          <mat-card class="card-surface trips-card">
            <h2>Bus-wise Seat Status</h2>
            @if (busStatus().length === 0) {
              <p class="empty-state">No trips are scheduled for today.</p>
            } @else {
              <table mat-table [dataSource]="busStatus()" class="mono-table">
                <ng-container matColumnDef="bus">
                  <th mat-header-cell *matHeaderCellDef>Bus</th>
                  <td mat-cell *matCellDef="let b"><span class="mono">{{ b.busNumber }}</span></td>
                </ng-container>
                <ng-container matColumnDef="route">
                  <th mat-header-cell *matHeaderCellDef>Route</th>
                  <td mat-cell *matCellDef="let b">{{ b.routeName }}</td>
                </ng-container>
                <ng-container matColumnDef="time">
                  <th mat-header-cell *matHeaderCellDef>Time</th>
                  <td mat-cell *matCellDef="let b"><span class="mono">{{ b.departureTime.slice(0, 5) }}</span></td>
                </ng-container>
                <ng-container matColumnDef="available">
                  <th mat-header-cell *matHeaderCellDef>Available</th>
                  <td mat-cell *matCellDef="let b">{{ b.availableSeats }} / {{ b.totalSeats }}</td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="busColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: busColumns"></tr>
              </table>
            }
          </mat-card>

          <mat-card class="card-surface trips-card">
            <h2>Route-wise Sales</h2>
            @if (routeSales().length === 0) {
              <p class="empty-state">No sales recorded for today.</p>
            } @else {
              <table mat-table [dataSource]="routeSales()" class="mono-table">
                <ng-container matColumnDef="route">
                  <th mat-header-cell *matHeaderCellDef>Route</th>
                  <td mat-cell *matCellDef="let r">{{ r.routeName }}</td>
                </ng-container>
                <ng-container matColumnDef="sold">
                  <th mat-header-cell *matHeaderCellDef>Sold</th>
                  <td mat-cell *matCellDef="let r">{{ r.soldTickets }}</td>
                </ng-container>
                <ng-container matColumnDef="available">
                  <th mat-header-cell *matHeaderCellDef>Available</th>
                  <td mat-cell *matCellDef="let r">{{ r.availableSeats }}</td>
                </ng-container>
                <ng-container matColumnDef="sales">
                  <th mat-header-cell *matHeaderCellDef>Sales</th>
                  <td mat-cell *matCellDef="let r">৳{{ r.totalSales }}</td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="routeColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: routeColumns"></tr>
              </table>
            }
          </mat-card>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .subtitle { color: var(--color-text-muted); margin-bottom: 20px; }
      .tiles { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 24px; }
      .tile { display: flex; align-items: center; gap: 14px; padding: 18px 20px; }
      .tile__icon { color: var(--color-accent-ink); background: var(--color-available-bg); border-radius: 50%; padding: 8px; width: 22px; height: 22px; }
      .tile__value { font-size: 1.4rem; font-weight: 600; }
      .tile__label { font-size: 0.78rem; color: var(--color-text-muted); }
      .two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
      @media (max-width: 900px) { .two-col { grid-template-columns: 1fr; } }
      .trips-card { padding: 20px; }
      table { width: 100%; }
      .loading-row { display: flex; justify-content: center; padding: 32px; }
      .empty-state { color: var(--color-text-muted); padding: 16px 0; text-align: center; }
    `,
  ],
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);

  protected readonly loading = signal(true);
  protected readonly summary = signal<DashboardSummaryDto | null>(null);
  protected readonly busColumns = ['bus', 'route', 'time', 'available'];
  protected readonly routeColumns = ['route', 'sold', 'available', 'sales'];

  protected readonly todayLabel = signal(
    new Date().toLocaleDateString('en-GB', { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' }),
  );

  protected readonly busStatus = computed<BusSeatStatusDto[]>(() => this.summary()?.busWiseSeatStatus ?? []);
  protected readonly routeSales = computed<RouteSalesDto[]>(() => this.summary()?.routeWiseSales ?? []);

  protected readonly tiles = computed<SummaryTile[]>(() => {
    const s = this.summary();
    if (!s) return [];
    return [
      { label: 'Total Seats', value: String(s.totalSeats), icon: 'event_seat' },
      { label: 'Sold Seats', value: String(s.soldSeats), icon: 'confirmation_number' },
      { label: 'Available Seats', value: String(s.availableSeats), icon: 'event_available' },
      { label: "Today's Sales", value: `৳${s.totalSales}`, icon: 'payments' },
    ];
  });

  ngOnInit(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.dashboardService.getSummary(today).subscribe((summary) => {
      this.summary.set(summary);
      this.loading.set(false);
    });
  }
}
