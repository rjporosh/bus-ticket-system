import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { MatCheckboxModule } from '@angular/material/checkbox';
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
  PaymentDto,
  PaymentStatus,
  PaymentMethod,
  PaymentMethodLabel,
  PaymentStatusLabel,
  SellTicketsRequest,
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
    MatCheckboxModule,
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
                        <ng-container matColumnDef="seats">
                          <th mat-header-cell *matHeaderCellDef>Seats</th>
                          <td mat-cell *matCellDef="let t">{{ t.availableSeats }} available</td>
                        </ng-container>
                        <ng-container matColumnDef="action">
                          <th mat-header-cell *matHeaderCellDef></th>
                          <td mat-cell *matCellDef="let t">
                            <button mat-flat-button color="primary" (click)="selectTrip(t)">Select</button>
                          </td>
                        </ng-container>
                        <tr mat-header-row *matHeaderRowDef="['bus', 'time', 'fare', 'seats', 'action']"></tr>
                        <tr mat-row *matRowDef="let row; columns: ['bus', 'time', 'fare', 'seats', 'action']"></tr>
                      </table>
                    } @else if (tripForm.value.routeId) {
                      <p class="empty-state">No trips run on this date for the selected route.</p>
                    }
                  </mat-card>
                }

                @case ('seat') {
                  <mat-card class="card-surface step-card">
                    <div class="step-header">
                      <h2>2. Select Seats — {{ selectedTrip()?.busNumber }} · {{ selectedTrip()?.departureTime?.slice(0, 5) }}</h2>
                      <button mat-button (click)="step.set('trip')"><mat-icon>arrow_back</mat-icon> Back</button>
                    </div>

                    @if (loadingSeats()) {
                      <div class="loading-row"><mat-spinner diameter="28" /></div>
                    } @else {
                      <div class="legend">
                        <span class="board-chip board-chip--available">Available</span>
                        <span class="board-chip board-chip--selected">Selected</span>
                        <span class="board-chip board-chip--sold">Sold</span>
                        <span class="board-chip board-chip--muted">Out of Service</span>
                        @if (seats().some(s => s.isDriver)) {
                          <span class="board-chip driver-chip">Driver</span>
                        }
                      </div>
                      <div class="selected-seats-info">
                        <span>Selected: {{ selectedSeats().length }} / 10</span>
                        @if (selectedSeats().length > 0) {
                          <button mat-button (click)="clearSeats()">Clear</button>
                        }
                      </div>
                      <div class="seat-grid" [style.gridTemplateColumns]="getGridTemplateColumns()">
                        @for (seat of seats(); track seat.seatId) {
                          <button
                            type="button"
                            class="seat"
                            [class.seat--sold]="seat.isSold"
                            [class.seat--inactive]="!seat.isInService"
                            [class.seat--selected]="isSelected(seat)"
                            [class.seat--driver]="seat.isDriver"
                            [class.seat--male]="seat.isSold && seat.passengerGender === 'Male'"
                            [class.seat--female]="seat.isSold && seat.passengerGender === 'Female'"
                            [disabled]="seat.isSold || !seat.isInService || seat.isDriver"
                            [matTooltip]="seatClassLabel(seat)"
                            [style.grid-row]="getVisualRow(seat)"
                            [style.grid-column]="getVisualCol(seat)"
                            (click)="toggleSeat(seat)"
                          >
                            @if (seat.isDriver) {
                              <span class="driver-icon">&#x1F69A;</span>
                            } @else if (seat.isSold && seat.passengerName) {
                              <span class="passenger-initials">{{ getInitials(seat.passengerName) }}</span>
                            } @else {
                              {{ seat.seatNumber }}
                            }
                          </button>
                        }
                      </div>
                      <button mat-flat-button color="primary" [disabled]="selectedSeats().length === 0" (click)="step.set('passenger')">
                        Continue with {{ selectedSeats().length }} Seat{{ selectedSeats().length > 1 ? 's' : '' }}
                      </button>
                    }
                  </mat-card>
                }

                @case ('passenger') {
                  <mat-card class="card-surface step-card">
                    <div class="step-header">
                      <h2>3. Passenger Information — {{ selectedSeats().length }} Seat{{ selectedSeats().length > 1 ? 's' : '' }}</h2>
                      <button mat-button (click)="step.set('seat')"><mat-icon>arrow_back</mat-icon> Back</button>
                    </div>
                    <form [formGroup]="bookingForm" (ngSubmit)="sellTickets()">
                      <div formArrayName="passengers">
                        @for (passenger of passengers.controls; track $index; let i = $index) {
                          <div [formGroupName]="i" class="passenger-card">
                            <h4>
                              @if (sameForAll) {
                                Passenger Details
                              } @else {
                                Passenger {{ i + 1 }} · Seat {{ selectedSeats()[i]?.seatNumber }}
                              }
                            </h4>
                            <div class="grid-2">
                              <mat-form-field appearance="outline">
                                <mat-label>Full Name *</mat-label>
                                <input matInput formControlName="name" placeholder="As per ID" />
                                <mat-error *ngIf="passenger.get('name')?.hasError('required')">Name is required</mat-error>
                              </mat-form-field>
                              <mat-form-field appearance="outline">
                                <mat-label>Mobile Number *</mat-label>
                                <input matInput formControlName="mobile" placeholder="01XXXXXXXXX" maxlength="11" />
                                <mat-error *ngIf="passenger.get('mobile')?.hasError('required')">Mobile is required</mat-error>
                                <mat-error *ngIf="passenger.get('mobile')?.hasError('pattern')">Numbers only, max 11 digits</mat-error>
                              </mat-form-field>
                              <mat-form-field appearance="outline">
                                <mat-label>Gender</mat-label>
                                <mat-select formControlName="gender">
                                  <mat-option value="Male">Male</mat-option>
                                  <mat-option value="Female">Female</mat-option>
                                  <mat-option value="Other">Other</mat-option>
                                </mat-select>
                              </mat-form-field>
                              <mat-form-field appearance="outline">
                                <mat-label>Age</mat-label>
                                <input matInput type="number" formControlName="age" min="0" max="120" />
                              </mat-form-field>
                              <mat-form-field appearance="outline">
                                <mat-label>NID / Passport</mat-label>
                                <input matInput formControlName="nid" />
                              </mat-form-field>
                            </div>
                          </div>
                        }
                      </div>

                      @if (selectedSeats().length > 1) {
                        <mat-checkbox formControlName="sameForAll" (change)="onSameForAllChange()" class="same-for-all">
                          Same passenger for all seats
                        </mat-checkbox>
                      }

                      <mat-form-field appearance="outline" class="full-width">
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
                        <button mat-flat-button color="primary" type="submit" [disabled]="bookingForm.invalid || selling()">
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
                  <mat-card class="card-surface step-card">
                    <div class="ticket-stub__header">
                      <mat-icon class="ticket-stub__check">check_circle</mat-icon>
                      <h2>Tickets Sold Successfully</h2>
                    </div>
                    @if (soldTickets().length > 0) {
                      <div class="sold-tickets-list">
                        @for (ticket of soldTickets(); track ticket.id) {
                          <div class="ticket-card">
                            <div class="ticket-card__header">
                              <strong>{{ ticket.ticketNumber }}</strong>
                              <span class="board-chip board-chip--sold">{{ ticketStatusLabel(ticket.status) }}</span>
                            </div>
                            <dl class="ticket-details mono">
                              <dt>Passenger</dt><dd>{{ ticket.passengerName }}</dd>
                              <dt>Mobile</dt><dd>{{ ticket.mobileNumber }}</dd>
                              <dt>Gender</dt><dd>{{ ticket.gender || '—' }}</dd>
                              <dt>Age</dt><dd>{{ ticket.age ?? '—' }}</dd>
                              <dt>Bus</dt><dd>{{ ticket.busNumber }}</dd>
                              <dt>Seat</dt><dd>{{ ticket.seatNumber }}</dd>
                              <dt>Travel Date</dt><dd>{{ ticket.travelDate }}</dd>
                              <dt>Departure</dt><dd>{{ ticket.departureTime?.slice(0, 5) }}</dd>
                              <dt>Fare</dt><dd>৳{{ ticket.fareAmount }}</dd>
                            </dl>
                          </div>
                        }
                      </div>
                    }
                    <div class="actions-row">
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

        <mat-tab label="Payments">
          <ng-template matTabContent>
            <div class="tab-body">
              <mat-card class="card-surface step-card">
                <form [formGroup]="paymentFilterForm" class="grid-3">
                  <mat-form-field appearance="outline">
                    <mat-label>Status</mat-label>
                    <mat-select formControlName="status">
                      <mat-option [value]="-1">All</mat-option>
                      <mat-option [value]="0">Pending</mat-option>
                      <mat-option [value]="1">Captured</mat-option>
                      <mat-option [value]="2">Failed</mat-option>
                      <mat-option [value]="3">Refunded</mat-option>
                    </mat-select>
                  </mat-form-field>
                  <mat-form-field appearance="outline">
                    <mat-label>Method</mat-label>
                    <mat-select formControlName="method">
                      <mat-option [value]="-1">All</mat-option>
                      <mat-option [value]="0">Cash</mat-option>
                      <mat-option [value]="1">Mock Card</mat-option>
                      <mat-option [value]="2">Mock Mobile Banking</mat-option>
                    </mat-select>
                  </mat-form-field>
                  <button mat-flat-button color="primary" type="button" (click)="loadPayments()">Filter</button>
                </form>

                @if (loadingPayments()) {
                  <div class="loading-row"><mat-spinner diameter="28" /></div>
                } @else if (payments().length > 0) {
                  <table mat-table [dataSource]="payments()" class="mono-table">
                    <ng-container matColumnDef="ticket">
                      <th mat-header-cell *matHeaderCellDef>Ticket</th>
                      <td mat-cell *matCellDef="let p"><span class="mono">{{ p.ticketNumber }}</span></td>
                    </ng-container>
                    <ng-container matColumnDef="passenger">
                      <th mat-header-cell *matHeaderCellDef>Passenger</th>
                      <td mat-cell *matCellDef="let p">{{ p.passengerName }}</td>
                    </ng-container>
                    <ng-container matColumnDef="amount">
                      <th mat-header-cell *matHeaderCellDef>Amount</th>
                      <td mat-cell *matCellDef="let p">৳{{ p.amount }}</td>
                    </ng-container>
                    <ng-container matColumnDef="method">
                      <th mat-header-cell *matHeaderCellDef>Method</th>
                      <td mat-cell *matCellDef="let p">{{ paymentMethodLabel(p.method) }}</td>
                    </ng-container>
                    <ng-container matColumnDef="status">
                       <th mat-header-cell *matHeaderCellDef>Status</th>
                      <td mat-cell *matCellDef="let p">
                        <span class="board-chip" [class.board-chip--available]="p.status === 1" [class.board-chip--sold]="p.status === 0">
                          {{ paymentStatusLabel(p.status) }}
                        </span>
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="ref">
                      <th mat-header-cell *matHeaderCellDef>Transaction Ref</th>
                      <td mat-cell *matCellDef="let p">{{ p.transactionRef }}</td>
                    </ng-container>
                    <ng-container matColumnDef="actions">
                      <th mat-header-cell *matHeaderCellDef></th>
                      <td mat-cell *matCellDef="let p">
                        @if (p.status === 0) {
                          <button mat-button (click)="capturePayment(p)" matTooltip="Capture">Capture</button>
                        }
                        @if (p.status === 1) {
                          <button mat-button (click)="refundPayment(p)" matTooltip="Refund">Refund</button>
                        }
                        @if (p.status === 0 || p.status === 1) {
                          <button mat-button color="warn" (click)="failPayment(p)" matTooltip="Fail">Fail</button>
                        }
                      </td>
                    </ng-container>

                    <tr mat-header-row *matHeaderRowDef="paymentColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: paymentColumns"></tr>
                  </table>
                } @else if (paymentsLoaded()) {
                  <p class="empty-state">No payments found.</p>
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

      .legend { display: flex; gap: 8px; margin-bottom: 16px; flex-wrap: wrap; }
      .selected-seats-info { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; color: var(--color-text-muted); font-size: 0.85rem; }
      .seat-grid {
        display: grid;
        gap: 0;
        justify-content: center;
        margin-bottom: 20px;
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
        display: flex;
        align-items: center;
        justify-content: center;
      }
      .seat:hover:not(.seat--sold):not(.seat--inactive):not(.seat--driver):not(.seat--selected) { transform: scale(1.05); }
      .seat--sold { border-color: var(--color-sold); background: var(--color-sold-bg); color: var(--color-sold); cursor: not-allowed; }
      .seat--inactive { border-color: var(--color-outofservice); background: var(--color-outofservice-bg); color: var(--color-outofservice); cursor: not-allowed; }
      .seat--selected { outline: 2px solid var(--color-accent); outline-offset: 2px; background: var(--color-accent); color: #fff; border-color: var(--color-accent); }
      .seat--male { background: #e3f2fd; border-color: #90caf9; color: #1565c0; }
      .seat--female { background: #fce4ec; border-color: #f48fb1; color: #c2185b; }
      .seat--driver { border-color: #ff9800; background: #fff3e0; color: #e65100; cursor: default; }
      .driver-icon { font-size: 1.1rem; }
      .passenger-initials { font-size: 0.6rem; font-weight: 700; }
      .driver-chip { background: #fff3e0; color: #e65100; border-color: #ff9800; }

      .passenger-card { margin-bottom: 1rem; padding: 1rem; border: 1px solid #e0e0e0; border-radius: 8px; background: #fafafa; }
      .passenger-card h4 { margin: 0 0 0.75rem; color: #333; font-size: 0.95rem; }
      .same-for-all { margin: 0.5rem 0 1rem; }

      .ticket-stub { border-left: 4px dashed var(--color-accent); max-width: 420px; }
      .ticket-stub__header { display: flex; align-items: center; gap: 10px; margin-bottom: 16px; }
      .ticket-stub__check { color: var(--color-available); }
      .sold-tickets-list { display: flex; flex-direction: column; gap: 16px; margin-bottom: 20px; }
      .ticket-card { border: 1px solid #e0e0e0; border-radius: 8px; padding: 16px; background: #fafafa; }
      .ticket-card__header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
      .ticket-details { display: grid; grid-template-columns: auto 1fr; gap: 4px 16px; }
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

  protected readonly step = signal<WizardStep>('trip');
  protected readonly routes = signal<RouteDto[]>([]);
  protected readonly trips = signal<TripDto[]>([]);
  protected readonly loadingTrips = signal(false);
  protected readonly selectedTrip = signal<TripDto | null>(null);
  protected readonly seats = signal<SeatAvailabilityDto[]>([]);
  protected readonly loadingSeats = signal(false);
  protected readonly selectedSeats = signal<SeatAvailabilityDto[]>([]);
  protected readonly lastSelectedSeatId = signal<string | null>(null);
  protected readonly selling = signal(false);
  protected readonly soldTickets = signal<TicketDto[]>([]);

  protected readonly searchForm = this.fb.nonNullable.group({
    searchBy: [TicketSearchField.TicketNumber],
    searchText: [''],
  });
  protected readonly searchResults = signal<TicketDto[]>([]);
  protected readonly searched = signal(false);
  protected readonly cancelling = signal<TicketDto | null>(null);
  protected readonly cancelReason = this.fb.nonNullable.control('');
  protected readonly searchColumns = ['ticketNumber', 'passenger', 'trip', 'date', 'status', 'actions'];

  protected readonly paymentFilterForm = this.fb.nonNullable.group({
    status: [-1],
    method: [-1],
  });
  protected readonly payments = signal<PaymentDto[]>([]);
  protected readonly loadingPayments = signal(false);
  protected readonly paymentsLoaded = signal(false);
  protected readonly paymentColumns = ['ticket', 'passenger', 'amount', 'method', 'status', 'ref', 'actions'];

  protected readonly tripForm = this.fb.nonNullable.group({
    routeId: [''],
    travelDate: [new Date().toISOString().slice(0, 10)],
  });

  protected readonly bookingForm = this.fb.nonNullable.group({
    sameForAll: [true],
    passengers: this.fb.array([
      this.fb.nonNullable.group({
        name: ['', Validators.required],
        mobile: ['', [Validators.required, Validators.pattern('^[0-9]{0,11}$'), Validators.maxLength(11)]],
        gender: [''],
        age: [null as number | null],
        nid: [''],
      }),
    ]),
    paymentMethod: [0, Validators.required],
    remarks: [''],
  });

  get passengers(): FormArray {
    return this.bookingForm.controls['passengers'] as FormArray;
  }

  get sameForAll(): boolean {
    return this.bookingForm.controls['sameForAll'].value;
  }

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
    this.selectedSeats.set([]);
    this.lastSelectedSeatId.set(null);
    this.step.set('seat');
    this.loadingSeats.set(true);

    const travelDate = this.tripForm.getRawValue().travelDate;
    this.bookingService.getAvailableSeats(trip.scheduleId, travelDate).subscribe({
      next: (seats) => {
        this.seats.set(seats);
        this.loadingSeats.set(false);
      },
      error: () => {
        this.toast.error('Failed to load seat map.');
        this.loadingSeats.set(false);
      },
    });
  }

  toggleSeat(seat: SeatAvailabilityDto): void {
    if (seat.isSold || !seat.isInService || seat.isDriver) return;

    const current = this.selectedSeats();
    const index = current.findIndex(s => s.seatId === seat.seatId);
    if (index >= 0) {
      current.splice(index, 1);
      this.lastSelectedSeatId.set(current.length > 0 ? current[current.length - 1].seatId : null);
    } else {
      if (current.length >= 10) {
        this.toast.error('You can select up to 10 seats.');
        return;
      }
      current.push(seat);
      this.lastSelectedSeatId.set(seat.seatId);
      if (current.length > 1) {
        const sameForAllControl = this.bookingForm.get('sameForAll');
        if (sameForAllControl?.value) {
          sameForAllControl.setValue(false);
        }
      }
    }
    this.selectedSeats.set([...current]);
    this.syncPassengers();
  }

  isSelected(seat: SeatAvailabilityDto): boolean {
    return this.selectedSeats().some(s => s.seatId === seat.seatId);
  }

  clearSeats(): void {
    this.selectedSeats.set([]);
    this.lastSelectedSeatId.set(null);
    this.syncPassengers();
  }

  getGridTemplateColumns(): string {
    const seats = this.seats();
    if (!seats.length) return 'repeat(4, 48px)';
    const maxCol = Math.max(...seats.map(s => s.visualCol ?? 1));
    return `repeat(${maxCol}, 48px)`;
  }

  getVisualRow(seat: SeatAvailabilityDto): number {
    return seat.visualRow ?? 0;
  }

  getVisualCol(seat: SeatAvailabilityDto): number {
    return seat.visualCol ?? 0;
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  createPassengerGroup(): FormGroup {
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      mobile: ['', [Validators.required, Validators.pattern('^[0-9]{0,11}$'), Validators.maxLength(11)]],
      gender: [''],
      age: [null as number | null],
      nid: [''],
    });
  }

  syncPassengers(): void {
    const count = this.sameForAll ? 1 : this.selectedSeats().length;
    while (this.passengers.length < count) {
      this.passengers.push(this.createPassengerGroup());
    }
    while (this.passengers.length > count) {
      this.passengers.removeAt(this.passengers.length - 1);
    }
  }

  onSameForAllChange(): void {
    this.syncPassengers();
  }

  seatClassLabel(seat: SeatAvailabilityDto): string {
    return `${SeatClassLabel[seat.class]}${seat.isSold ? ' · Sold' : seat.isInService ? ' · Available' : ' · Out of service'}`;
  }

  sellTickets(): void {
    if (this.bookingForm.invalid || this.selectedSeats().length === 0 || !this.selectedTrip()) return;

    this.selling.set(true);
    const value = this.bookingForm.getRawValue();
    const trip = this.selectedTrip()!;
    const travelDate = this.tripForm.getRawValue().travelDate;
    const selected = this.selectedSeats();
    const passengers = value.passengers as any[];

    const request: SellTicketsRequest = {
      scheduleId: trip.scheduleId,
      travelDate,
      items: selected.map((seat, i) => {
        const p = passengers[Math.min(i, passengers.length - 1)];
        return {
          seatId: seat.seatId,
          passengerName: p.name,
          mobileNumber: p.mobile,
          fareAmount: trip.fareAmount,
          paymentMethod: value.paymentMethod as PaymentMethod,
          nidOrPassport: p.nid || undefined,
          gender: p.gender || undefined,
          age: p.age || undefined,
        };
      }),
      remarks: value.remarks || undefined,
    };

    this.bookingService.sellTickets(request).subscribe({
      next: (tickets) => {
        this.selling.set(false);
        this.soldTickets.set(tickets);
        this.step.set('confirmation');
      },
      error: (err: HttpErrorResponse) => {
        this.selling.set(false);
        const problem = err.error as ProblemDetails | undefined;
        this.toast.error(problem?.title ?? problem?.detail ?? 'Could not complete sale. Please try again.');
        if (err.status === 409) {
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
    this.selectedSeats.set([]);
    this.lastSelectedSeatId.set(null);
    this.soldTickets.set([]);
    this.bookingForm.reset({ sameForAll: true, paymentMethod: 0, remarks: '' });
    this.syncPassengers();
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

  paymentStatusLabel(status: PaymentDto['status']): string {
    return PaymentStatusLabel[status];
  }

  paymentMethodLabel(method: PaymentDto['method']): string {
    return PaymentMethodLabel[method];
  }

  loadPayments(): void {
    this.loadingPayments.set(true);
    this.paymentsLoaded.set(false);
    const { status, method } = this.paymentFilterForm.getRawValue();
    const query: any = { pageSize: 50 };
    if (status !== -1) query.status = status;
    if (method !== -1) query.method = method;

    this.bookingService.getPayments(query).subscribe({
      next: (result) => {
        this.payments.set(result.items);
        this.loadingPayments.set(false);
        this.paymentsLoaded.set(true);
      },
      error: () => {
        this.payments.set([]);
        this.loadingPayments.set(false);
        this.paymentsLoaded.set(true);
      },
    });
  }

  capturePayment(payment: PaymentDto): void {
    this.bookingService.capturePayment(payment.id).subscribe({
      next: () => {
        this.toast.success(`Payment ${payment.transactionRef} captured.`);
        this.loadPayments();
      },
      error: (error: HttpErrorResponse) => {
        const problem = error.error as ProblemDetails | undefined;
        this.toast.error(problem?.title ?? problem?.detail ?? 'Could not capture payment.');
      },
    });
  }

  refundPayment(payment: PaymentDto): void {
    this.bookingService.refundPayment(payment.id).subscribe({
      next: () => {
        this.toast.success(`Payment ${payment.transactionRef} refunded.`);
        this.loadPayments();
      },
      error: (error: HttpErrorResponse) => {
        const problem = error.error as ProblemDetails | undefined;
        this.toast.error(problem?.title ?? problem?.detail ?? 'Could not refund payment.');
      },
    });
  }

  failPayment(payment: PaymentDto): void {
    if (!confirm(`Mark payment ${payment.transactionRef} as failed? This action cannot be undone.`)) return;
    this.bookingService.failPayment(payment.id).subscribe({
      next: () => {
        this.toast.success(`Payment ${payment.transactionRef} marked as failed.`);
        this.loadPayments();
      },
      error: (error: HttpErrorResponse) => {
        const problem = error.error as ProblemDetails | undefined;
        this.toast.error(problem?.title ?? problem?.detail ?? 'Could not fail payment.');
      },
    });
  }
}
