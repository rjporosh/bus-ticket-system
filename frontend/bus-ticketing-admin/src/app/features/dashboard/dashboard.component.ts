import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin } from 'rxjs';
import { BusesService, RoutesService, SchedulesService, StationsService } from '../../core/services/feature-services';
import { TripDto } from '../../core/models/api-models';

interface SummaryTile {
  label: string;
  value: number;
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

      <mat-card class="card-surface trips-card">
        <h2>Today's Trips</h2>

        @if (loading()) {
          <div class="loading-row"><mat-spinner diameter="28" /></div>
        } @else if (trips().length === 0) {
          <p class="empty-state">No trips are scheduled for today.</p>
        } @else {
          <table mat-table [dataSource]="trips()" class="mono-table">
            <ng-container matColumnDef="bus">
              <th mat-header-cell *matHeaderCellDef>Bus</th>
              <td mat-cell *matCellDef="let t">{{ t.busNumber }}</td>
            </ng-container>
            <ng-container matColumnDef="route">
              <th mat-header-cell *matHeaderCellDef>Route</th>
              <td mat-cell *matCellDef="let t">{{ t.routeName }}</td>
            </ng-container>
            <ng-container matColumnDef="departure">
              <th mat-header-cell *matHeaderCellDef>Departure</th>
              <td mat-cell *matCellDef="let t"><span class="mono">{{ t.departureTime.slice(0, 5) }}</span></td>
            </ng-container>
            <ng-container matColumnDef="arrival">
              <th mat-header-cell *matHeaderCellDef>Arrival</th>
              <td mat-cell *matCellDef="let t"><span class="mono">{{ t.arrivalTime.slice(0, 5) }}</span></td>
            </ng-container>
            <ng-container matColumnDef="seats">
              <th mat-header-cell *matHeaderCellDef>Seats</th>
              <td mat-cell *matCellDef="let t">{{ t.totalSeats }}</td>
            </ng-container>
            <ng-container matColumnDef="fare">
              <th mat-header-cell *matHeaderCellDef>Fare</th>
              <td mat-cell *matCellDef="let t">৳{{ t.fareAmount }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columns"></tr>
            <tr mat-row *matRowDef="let row; columns: columns"></tr>
          </table>
        }
      </mat-card>
    </div>
  `,
  styles: [
    `
      .subtitle {
        color: var(--color-text-muted);
        margin-bottom: 20px;
      }
      .tiles {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 16px;
        margin-bottom: 24px;
      }
      .tile {
        display: flex;
        align-items: center;
        gap: 14px;
        padding: 18px 20px;
      }
      .tile__icon {
        color: var(--color-accent-ink);
        background: var(--color-available-bg);
        border-radius: 50%;
        padding: 8px;
        width: 22px;
        height: 22px;
      }
      .tile__value {
        font-size: 1.4rem;
        font-weight: 600;
      }
      .tile__label {
        font-size: 0.78rem;
        color: var(--color-text-muted);
      }
      .trips-card {
        padding: 20px;
      }
      table {
        width: 100%;
      }
      .loading-row {
        display: flex;
        justify-content: center;
        padding: 32px;
      }
      .empty-state {
        color: var(--color-text-muted);
        padding: 24px 0;
        text-align: center;
      }
    `,
  ],
})
export class DashboardComponent implements OnInit {
  private readonly schedulesService = inject(SchedulesService);
  private readonly busesService = inject(BusesService);
  private readonly routesService = inject(RoutesService);
  private readonly stationsService = inject(StationsService);

  protected readonly loading = signal(true);
  protected readonly trips = signal<TripDto[]>([]);
  protected readonly columns = ['bus', 'route', 'departure', 'arrival', 'seats', 'fare'];

  private readonly busCount = signal(0);
  private readonly routeCount = signal(0);
  private readonly stationCount = signal(0);

  protected readonly todayLabel = signal(
    new Date().toLocaleDateString('en-GB', { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' }),
  );

  protected readonly tiles = computed<SummaryTile[]>(() => [
    { label: "Today's Trips", value: this.trips().length, icon: 'event' },
    { label: 'Active Buses', value: this.busCount(), icon: 'directions_bus' },
    { label: 'Routes', value: this.routeCount(), icon: 'alt_route' },
    { label: 'Stations', value: this.stationCount(), icon: 'location_on' },
  ]);

  ngOnInit(): void {
    const today = new Date().toISOString().slice(0, 10);

    forkJoin({
      trips: this.schedulesService.getTripsForDate(today),
      buses: this.busesService.list({ isActive: true, pageSize: 1 }),
      routes: this.routesService.list({ isActive: true, pageSize: 1 }),
      stations: this.stationsService.list({ isActive: true, pageSize: 1 }),
    }).subscribe(({ trips, buses, routes, stations }) => {
      this.trips.set(trips);
      this.busCount.set(buses.totalCount);
      this.routeCount.set(routes.totalCount);
      this.stationCount.set(stations.totalCount);
      this.loading.set(false);
    });
  }
}
