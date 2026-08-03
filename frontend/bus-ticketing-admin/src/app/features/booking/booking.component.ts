import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpErrorResponse } from '@angular/common/http';
import { BookingService, RoutesService, SchedulesService } from '../../core/services/feature-services';
import { ToastService } from '../../core/services/toast.service';
import {
  ProblemDetails,
  RouteDto,
  SeatAvailabilityDto,
  SeatClassLabel,
  TicketDto,
  TicketSearchField,
  TicketStatusLabel,
  TripDto,
} from '../../core/models/api-models';

type WizardStep = 'trip' | 'seat' | 'passenger' | 'confirmation';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTabsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <h1>Ticketing</h1>
      <mat-tab-group animationDuration="150ms">
        <mat-tab label="Sell Ticket">
          <ng-template matTabContent>
            <div class="tab-body">
              @switch (step()) {
                @case ('trip') {
                  <mat-card class="card-surface step-card">
                    <h2>1. Select Trip</h2>
                    <form [formGroup]="tripForm" class="grid-3">
                      <mat-form-field appearance="outline">
                        <mat-label>Route</mat-label>
                        <mat-select formControlName="routeId" (selectionChange)="loadTrips()">
                          @for (r of routes(); track r.id) {
                            <mat-option [value]="r.id">{{ r.name }}</mat-option>
                          }
                        </mat-select>
                      </mat-form-field>
                      <mat-form-field appearance="outline">
                        <mat-label>Travel Date</mat-label>
                        <input matInput type="date" formControlName="travelDate" (change)="loadTrips()" />
                      </mat-form-field>
                    </form>

                    @if (loadingTrips()) {
                      <div class="loading-row"><mat-spinner diameter="28" /></div>
                    } @else if (trips().length > 0) {
                      <table mat-table [dataSource]="trips()" class="mono-table">
                        <ng-container matColumnDef="bus">
                          <th mat-header-cell *matHeaderCellDef>Bus</th>
                          <td mat-cell *matCellDef="let t">{{ t.busNumber }}</td>
                        </ng-container>
                        <ng-container matColumnDef="time">
                          <th mat-header-cell *matHeaderCellDef>Time</th>
                          <td mat-cell *matCellDef="let t"><span class="mono">{{ t.departureTime.slice(0, 5) }}</span></td>
                        </ng-container>
                        <ng-container matColumnDef="fare">
                          <th mat-header-cell *matHeaderCellDef>Fare</th>
                          <td mat-cell *matCellDef="let t">৳{{ t.fareAmount }}</td>
                        </ng-container>
                        <ng-container matColumnDef="action">
                          <th mat-header-cell *matHeaderCellDef></th>
                          <td mat-cell *matCellDef="let t">
                            <button mat-flat-button color="primary" (click)="selectTrip(t)">Select</button>
                          </td>
                        </ng-container>
                        <tr mat-header-row *matHeaderRowDef="['bus', 'time', 'fare', 'action']"></tr>
                        <tr mat-row *matRowDef="let row; columns: ['bus', 'time', 'fare', 'action']"></tr>
                      </table>
                    } @else if (tripForm.value.routeId) {
                      <p class="empty-state">No trips run on this date for the selected route.</p>
                    }
                  </mat-card>
                }

                @case ('seat') {
                  <mat-card class="card-surface step-card">
                    <div class="step-header">
                      <h2>2. Select Seat — {{ selectedTrip()?.busNumber }} · {{ selectedTrip()?.departureTime?.slice(0, 5) }}</h2>
                      <button mat-button (click)="step.set('trip')"><mat-icon>arrow_back</mat-icon> Back</button>
                    </div>

                    @if (loadingSeats()) {
                      <div class="loading-row"><mat-spinner diameter="28" /></div>
                    } @else {
                      <div class="legend">
                        <span class="board-chip board-chip--available">Available</span>
                        <span class="board-chip board-chip--sold">Sold</span>
                        <span class="board-chip board-chip--muted">Out of Service</span>
                      </div>
                      <div class="seat-grid">
                        @for (seat of seats(); track seat.seatId) {
                          <button
                            type="button"
                            class="seat"
                            [class.seat--sold]="seat.isSold"
                            [class.seat--inactive]="!seat.isInService"
                            [class.seat--selected]="selectedSeat()?.seatId === seat.seatId"
                            [disabled]="seat.isSold || !seat.isInService"
                            [matTooltip]="seatClassLabel(seat)"
                            (click)="selectedSeat.set(seat)"
                          >
                            {{ seat.seatNumber }}
                          </button>
                        }
                      </div>
                      <button mat-flat-button color="primary" [disabled]="!selectedSeat()" (click)="step.set('passenger')">
                        Continue with Seat {{ selectedSeat()?.seatNumber }}
                      </button>
                    }
                  </mat-card>
                }

                @case ('passenger') {
                  <mat-card class="card-surface step-card">
                    <div class="step-header">
                      <h2>3. Passenger Information — Seat {{ selectedSeat()?.seatNumber }}</h2>
                      <button mat-button (click)="step.set('seat')"><mat-icon>arrow_back</mat-icon> Back</button>
                    </div>
                    <form [formGroup]="passengerForm" (ngSubmit)="sellTicket()" class="grid-2">
                      <mat-form-field appearance="outline">
                        <mat-label>Passenger Name *</mat-label>
                        <input matInput formControlName="passengerName" />
                      </mat-form-field>
                      <mat-form-field appearance="outline">
                        <mat-label>Mobile Number *</mat-label>
                        <input matInput formControlName="mobileNumber" />
                      </mat-form-field>
                      <mat-form-field appearance="outline">
                        <mat-label>NID / Passport (optional)</mat-label>
                        <input matInput formControlName="nidOrPassport" />
                      </mat-form-field>
                      <mat-form-field appearance="outline">
                        <mat-label>Gender (optional)</mat-label>
                        <mat-select formControlName="gender">
                          <mat-option value="Male">Male</mat-option>
                          <mat-option value="Female">Female</mat-option>
                          <mat-option value="Other">Other</mat-option>
                        </mat-select>
                      </mat-form-field>
                      <mat-form-field appearance="outline">
                        <mat-label>Payment Method</mat-label>
                        <mat-select formControlName="paymentMethod">
                          <mat-option [value]="0">Cash</mat-option>
                          <mat-option [value]="1">Mock Card</mat-option>
                          <mat-option [value]="2">Mock Mobile Banking</mat-option>
                        </mat-select>
                      </mat-form-field>
                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>Remarks (optional)</mat-label>
                        <input matInput formControlName="remarks" />
                      </mat-form-field>

                      <div class="grid-span-2 actions-row">
                        <button mat-button type="button" (click)="step.set('seat')">Back</button>
                        <button mat-flat-button color="primary" type="submit" [disabled]="passengerForm.invalid || selling()">
                          @if (selling()) {
                            <mat-spinner diameter="18" />
                          } @else {
                            Confirm &amp; Complete Sale
                          }
                        </button>
                      </div>
                    </form>
                  </mat-card>
                }

                @case ('confirmation') {
                  <mat-card class="card-surface step-card ticket-stub">
                    <div class="ticket-stub__header">
                      <mat-icon class="ticket-stub__check">check_circle</mat-icon>
                      <h2>Ticket Sold Successfully</h2>
                    </div>
                    @if (soldTicket(); as t) {
                      <dl class="ticket-details mono">
                        <dt>Ticket No.</dt><dd>{{ t.ticketNumber }}</dd>
                        <dt>Passenger</dt><dd>{{ t.passengerName }}</dd>
                        <dt>Mobile</dt><dd>{{ t.mobileNumber }}</dd>
                        <dt>Route</dt><dd>{{ t.routeName }}</dd>
                        <dt>Bus</dt><dd>{{ t.busNumber }}</dd>
                        <dt>Seat</dt><dd>{{ t.seatNumber }}</dd>
                        <dt>Travel Date</dt><dd>{{ t.travelDate }}</dd>
                        <dt>Departure</dt><dd>{{ t.departureTime.slice(0, 5) }}</dd>
                        <dt>Fare</dt><dd>৳{{ t.fareAmount }}</dd>
                        <dt>Status</dt><dd>{{ ticketStatusLabel(t.status) }}</dd>
                      </dl>
                    }
                    <div class="actions-row">
                      <button mat-button (click)="printTicket()">
                        <mat-icon>print</mat-icon>
                        Print Ticket
                      </button>
                      <button mat-flat-button color="primary" (click)="resetWizard()">Sell Another Ticket</button>
                    </div>
                  </mat-card>
                }
              }
            </div>
          </ng-template>
        </mat-tab>

        <mat-tab label="Search &amp; Cancel">
          <ng-template matTabContent>
            <div class="tab-body">
              <mat-card class="card-surface step-card">
                <form [formGroup]="searchForm" (ngSubmit)="search()" class="grid-3">
                  <mat-form-field appearance="outline">
                    <mat-label>Search By</mat-label>
                    <mat-select formControlName="searchBy">
                      <mat-option [value]="0">Ticket Number</mat-option>
                      <mat-option [value]="1">Passenger Mobile</mat-option>
                    </mat-select>
                  </mat-form-field>
                  <mat-form-field appearance="outline">
                    <mat-label>Search Text</mat-label>
                    <input matInput formControlName="searchText" />
                  </mat-form-field>
                  <button mat-flat-button color="primary" type="submit">
                    <mat-icon>search</mat-icon>
                    Search
                  </button>
                </form>

                @if (searchResults().length > 0) {
                  <table mat-table [dataSource]="searchResults()" class="mono-table">
                    <ng-container matColumnDef="ticketNumber">
                      <th mat-header-cell *matHeaderCellDef>Ticket No.</th>
                      <td mat-cell *matCellDef="let t"><span class="mono">{{ t.ticketNumber }}</span></td>
                    </ng-container>
                    <ng-container matColumnDef="passenger">
                      <th mat-header-cell *matHeaderCellDef>Passenger</th>
                      <td mat-cell *matCellDef="let t">{{ t.passengerName }}</td>
                    </ng-container>
                    <ng-container matColumnDef="trip">
                      <th mat-header-cell *matHeaderCellDef>Trip</th>
                      <td mat-cell *matCellDef="let t">{{ t.routeName }} · {{ t.busNumber }} · Seat {{ t.seatNumber }}</td>
                    </ng-container>
                    <ng-container matColumnDef="date">
                      <th mat-header-cell *matHeaderCellDef>Travel Date</th>
                      <td mat-cell *matCellDef="let t">{{ t.travelDate }}</td>
                    </ng-container>
                    <ng-container matColumnDef="status">
                      <th mat-header-cell *matHeaderCellDef>Status</th>
                      <td mat-cell *matCellDef="let t">
                        <span class="board-chip" [class.board-chip--available]="t.status === 0" [class.board-chip--sold]="t.status === 1">
                          {{ ticketStatusLabel(t.status) }}
                        </span>
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="actions">
                      <th mat-header-cell *matHeaderCellDef></th>
                      <td mat-cell *matCellDef="let t">
                        @if (t.status === 0) {
                          <button mat-icon-button (click)="openCancel(t)" matTooltip="Cancel ticket" aria-label="Cancel ticket">
                            <mat-icon>cancel</mat-icon>
                          </button>
                        }
                      </td>
                    </ng-container>

                    <tr mat-header-row *matHeaderRowDef="searchColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: searchColumns"></tr>
                  </table>
                } @else if (searched()) {
                  <p class="empty-state">No tickets found.</p>
                }

                @if (cancelling(); as ticket) {
                  <div class="cancel-panel">
                    <h3>Cancel Ticket {{ ticket.ticketNumber }}</h3>
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Cancellation Reason</mat-label>
                      <input matInput [formControl]="cancelReason" placeholder="e.g. Passenger changed travel plan" />
                    </mat-form-field>
                    <div class="actions-row">
                      <button mat-button (click)="cancelling.set(null)">Dismiss</button>
                      <button mat-flat-button color="warn" [disabled]="!cancelReason.value" (click)="confirmCancel(ticket)">
                        Confirm Cancellation
                      </button>
                    </div>
                  </div>
                }
              </mat-card>
            </div>
          </ng-template>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [
    `
      .tab-body { padding-top: 16px; }
      .step-card { padding: 20px; }
      .step-header { display: flex; align-items: center; justify-content: space-between; }
      .grid-3 { display: grid; grid-template-columns: 1fr 1fr auto; gap: 12px; align-items: start; margin-bottom: 16px; }
      .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
      .grid-span-2 { grid-column: span 2; }
      .full-width { width: 100%; grid-column: span 2; }
      .actions-row { display: flex; justify-content: flex-end; gap: 8px; margin-top: 8px; }
      .loading-row { display: flex; justify-content: center; padding: 24px; }
      .empty-state { color: var(--color-text-muted); padding: 16px 0; }
      table { width: 100%; margin-top: 12px; }
      .legend { display: flex; gap: 8px; margin-bottom: 16px; }
      .seat-grid {
        display: grid;
        grid-template-columns: repeat(4, 48px);
        gap: 10px;
        margin-bottom: 20px;
        max-width: 260px;
      }
      .seat {
        font-family: var(--font-mono);
        font-size: 0.75rem;
        font-weight: 600;
        width: 48px;
        height: 44px;
        border-radius: var(--radius-sm);
        border: 1px solid var(--color-available);
        background: var(--color-available-bg);
        color: var(--color-available);
        cursor: pointer;
      }
      .seat--sold { border-color: var(--color-sold); background: var(--color-sold-bg); color: var(--color-sold); cursor: not-allowed; }
      .seat--inactive { border-color: var(--color-outofservice); background: var(--color-outofservice-bg); color: var(--color-outofservice); cursor: not-allowed; }
      .seat--selected { outline: 2px solid var(--color-accent); outline-offset: 2px; }
      .ticket-stub { border-left: 4px dashed var(--color-accent); max-width: 420px; }
      .ticket-stub__header { display: flex; align-items: center; gap: 10px; margin-bottom: 16px; }
      .ticket-stub__check { color: var(--color-available); }
      .ticket-details { display: grid; grid-template-columns: auto 1fr; gap: 4px 16px; margin-bottom: 20px; }
      .ticket-details dt { color: var(--color-text-muted); }
      .ticket-details dd { margin: 0; font-weight: 600; }
      .cancel-panel { margin-top: 20px; padding-top: 16px; border-top: 1px solid var(--color-border); max-width: 420px; }
    `,
  ],
})
export class BookingComponent {
  private readonly fb = inject(FormBuilder);
  private readonly bookingService = inject(BookingService);
  private readonly routesService = inject(RoutesService);
  private readonly schedulesService = inject(SchedulesService);
  private readonly toast = inject(ToastService);

  // --- Sell Ticket wizard state ---
  protected readonly step = signal<WizardStep>('trip');
  protected readonly routes = signal<RouteDto[]>([]);
  protected readonly trips = signal<TripDto[]>([]);
  protected readonly loadingTrips = signal(false);
  protected readonly selectedTrip = signal<TripDto | null>(null);
  protected readonly seats = signal<SeatAvailabilityDto[]>([]);
  protected readonly loadingSeats = signal(false);
  protected readonly selectedSeat = signal<SeatAvailabilityDto | null>(null);
  protected readonly selling = signal(false);
  protected readonly soldTicket = signal<TicketDto | null>(null);

  protected readonly tripForm = this.fb.nonNullable.group({
    routeId: [''],
    travelDate: [new Date().toISOString().slice(0, 10)],
  });

  protected readonly passengerForm = this.fb.nonNullable.group({
    passengerName: ['', Validators.required],
    mobileNumber: ['', Validators.required],
    nidOrPassport: [''],
    gender: [''],
    paymentMethod: [0, Validators.required],
    remarks: [''],
  });

  // --- Search & Cancel state ---
  protected readonly searchForm = this.fb.nonNullable.group({
    searchBy: [TicketSearchField.TicketNumber],
    searchText: [''],
  });
  protected readonly searchResults = signal<TicketDto[]>([]);
  protected readonly searched = signal(false);
  protected readonly cancelling = signal<TicketDto | null>(null);
  protected readonly cancelReason = this.fb.nonNullable.control('');
  protected readonly searchColumns = ['ticketNumber', 'passenger', 'trip', 'date', 'status', 'actions'];

  constructor() {
    this.routesService.list({ isActive: true, pageSize: 200 }).subscribe((result) => this.routes.set(result.items));
  }

  loadTrips(): void {
    const { routeId, travelDate } = this.tripForm.getRawValue();
    if (!routeId) return;

    this.loadingTrips.set(true);
    this.schedulesService.getTripsForDate(travelDate, routeId).subscribe((trips) => {
      this.trips.set(trips);
      this.loadingTrips.set(false);
    });
  }

  selectTrip(trip: TripDto): void {
    this.selectedTrip.set(trip);
    this.selectedSeat.set(null);
    this.step.set('seat');
    this.loadingSeats.set(true);

    const travelDate = this.tripForm.getRawValue().travelDate;
    this.bookingService.getAvailableSeats(trip.scheduleId, travelDate).subscribe((seats) => {
      this.seats.set(seats);
      this.loadingSeats.set(false);
    });
  }

  seatClassLabel(seat: SeatAvailabilityDto): string {
    return `${SeatClassLabel[seat.class]}${seat.isSold ? ' · Sold' : seat.isInService ? ' · Available' : ' · Out of service'}`;
  }

  sellTicket(): void {
    const trip = this.selectedTrip();
    const seat = this.selectedSeat();
    if (!trip || !seat || this.passengerForm.invalid) return;

    this.selling.set(true);
    const value = this.passengerForm.getRawValue();
    const travelDate = this.tripForm.getRawValue().travelDate;

    this.bookingService
      .sellTicket({
        scheduleId: trip.scheduleId,
        seatId: seat.seatId,
        travelDate,
        passengerName: value.passengerName,
        mobileNumber: value.mobileNumber,
        fareAmount: trip.fareAmount,
        paymentMethod: value.paymentMethod,
        nidOrPassport: value.nidOrPassport || null,
        gender: value.gender || null,
        remarks: value.remarks || null,
      })
      .subscribe({
        next: (ticket) => {
          this.selling.set(false);
          this.soldTicket.set(ticket);
          this.step.set('confirmation');
        },
        error: (error: HttpErrorResponse) => {
          this.selling.set(false);
          const problem = error.error as ProblemDetails | undefined;
          this.toast.error(problem?.title ?? problem?.detail ?? 'Could not sell this ticket.');
          if (error.status === 409) {
            // Someone else sold this seat first - refresh the seat map so the wizard reflects reality.
            this.selectTrip(trip);
          }
        },
      });
  }

  printTicket(): void {
    window.print();
  }

  resetWizard(): void {
    this.step.set('trip');
    this.selectedTrip.set(null);
    this.selectedSeat.set(null);
    this.soldTicket.set(null);
    this.passengerForm.reset({ paymentMethod: 0 });
    this.loadTrips();
  }

  ticketStatusLabel(status: TicketDto['status']): string {
    return TicketStatusLabel[status];
  }

  search(): void {
    const { searchBy, searchText } = this.searchForm.getRawValue();
    if (!searchText) return;

    this.bookingService.search({ searchBy, searchText, pageSize: 20 }).subscribe((result) => {
      this.searchResults.set(result.items);
      this.searched.set(true);
    });
  }

  openCancel(ticket: TicketDto): void {
    this.cancelReason.reset('');
    this.cancelling.set(ticket);
  }

  confirmCancel(ticket: TicketDto): void {
    this.bookingService.cancelTicket(ticket.id, this.cancelReason.value).subscribe({
      next: () => {
        this.toast.success(`Ticket ${ticket.ticketNumber} cancelled. Seat ${ticket.seatNumber} is now available.`);
        this.cancelling.set(null);
        this.search();
      },
      error: (error: HttpErrorResponse) => {
        const problem = error.error as ProblemDetails | undefined;
        this.toast.error(problem?.title ?? problem?.detail ?? 'Could not cancel this ticket.');
      },
    });
  }
}
