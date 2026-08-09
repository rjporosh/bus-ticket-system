import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ApiService } from '../../core/services/api.service';
import { TripsService } from '../../core/services/trips.service';
import { BookingService } from '../../core/services/booking.service';
import { ToastService } from '../../core/services/toast.service';
import {
  TripDto,
  SeatAvailabilityDto,
  TicketDto,
  SellTicketsRequest,
  PaymentMethod,
  SeatClass,
  SeatClassLabel,
  TicketStatus,
  TicketStatusLabel,
} from '../../core/models/api-models';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatSelectModule, MatIconModule, MatProgressSpinnerModule,
    MatSnackBarModule, MatCheckboxModule,
  ],
  template: `
    <div class="booking-container" *ngIf="trip(); else noTrip">
      <div class="booking-layout">
        <div class="trip-summary">
          <h2>Confirm Your Booking</h2>
          <div class="summary-card">
            <div class="summary-row">
              <span class="label">Route</span>
              <span class="value">{{ trip()!.routeName }}</span>
            </div>
            <div class="summary-row">
              <span class="label">Bus</span>
              <span class="value">{{ trip()!.busNumber }}</span>
            </div>
            <div class="summary-row">
              <span class="label">Date</span>
              <span class="value">{{ travelDate() | date:'fullDate' }}</span>
            </div>
            <div class="summary-row">
              <span class="label">Departure</span>
              <span class="value">{{ trip()!.departureTime }}</span>
            </div>
            <div class="summary-row">
              <span class="label">Arrival</span>
              <span class="value">{{ trip()!.arrivalTime }}</span>
            </div>
            <div class="summary-row total">
              <span class="label">Total Fare</span>
              <span class="value price">৳{{ selectedSeats().length * trip()!.fareAmount | number:'1.2-2' }}</span>
            </div>
          </div>

          <div class="selected-seats">
            <h4>Selected Seats ({{ selectedSeats().length }})</h4>
            <div class="seat-chips">
              <span class="seat-chip" *ngFor="let seat of selectedSeats()">{{ seat.seatNumber }}</span>
            </div>
          </div>
        </div>

        <div class="booking-form">
          <h3>Passenger Details</h3>
          <form [formGroup]="bookingForm" (ngSubmit)="onSubmit()">
            @if (sameForAll && selectedSeats().length > 0) {
              <div formArrayName="passengers">
                <div [formGroupName]="0" class="passenger-card">
                  <h4>Passenger Details (all {{ selectedSeats().length }} seats)</h4>
                  <div class="grid-2">
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Full Name *</mat-label>
                      <input matInput formControlName="name" placeholder="As per ID">
                      <mat-error *ngIf="passengers.at(0).get('name')?.hasError('required')">Name is required</mat-error>
                    </mat-form-field>
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Mobile Number *</mat-label>
                      <input matInput formControlName="mobile" placeholder="01XXXXXXXXX" maxlength="11">
                      <mat-error *ngIf="passengers.at(0).get('mobile')?.hasError('required')">Mobile is required</mat-error>
                      <mat-error *ngIf="passengers.at(0).get('mobile')?.hasError('pattern')">Numbers only, max 11 digits</mat-error>
                    </mat-form-field>
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Gender</mat-label>
                      <mat-select formControlName="gender">
                        <mat-option value="Male">Male</mat-option>
                        <mat-option value="Female">Female</mat-option>
                        <mat-option value="Other">Other</mat-option>
                      </mat-select>
                    </mat-form-field>
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Age</mat-label>
                      <input matInput type="number" formControlName="age" min="0" max="120" />
                    </mat-form-field>
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>NID / Passport</mat-label>
                      <input matInput formControlName="nid">
                    </mat-form-field>
                  </div>
                </div>
              </div>
            } @else {
              <div formArrayName="passengers">
                @for (passenger of passengers.controls; track $index; let i = $index) {
                  <div [formGroupName]="i" class="passenger-card">
                    <h4>
                      Passenger {{ i + 1 }} · Seat {{ selectedSeats()[i]?.seatNumber }}
                    </h4>
                    <div class="grid-2">
                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>Full Name *</mat-label>
                        <input matInput formControlName="name" placeholder="As per ID">
                        <mat-error *ngIf="passenger.get('name')?.hasError('required')">Name is required</mat-error>
                      </mat-form-field>
                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>Mobile Number *</mat-label>
                        <input matInput formControlName="mobile" placeholder="01XXXXXXXXX" maxlength="11">
                        <mat-error *ngIf="passenger.get('mobile')?.hasError('required')">Mobile is required</mat-error>
                        <mat-error *ngIf="passenger.get('mobile')?.hasError('pattern')">Numbers only, max 11 digits</mat-error>
                      </mat-form-field>
                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>Gender</mat-label>
                        <mat-select formControlName="gender">
                          <mat-option value="Male">Male</mat-option>
                          <mat-option value="Female">Female</mat-option>
                          <mat-option value="Other">Other</mat-option>
                        </mat-select>
                      </mat-form-field>
                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>Age</mat-label>
                        <input matInput type="number" formControlName="age" min="0" max="120" />
                      </mat-form-field>
                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>NID / Passport</mat-label>
                        <input matInput formControlName="nid">
                      </mat-form-field>
                    </div>
                  </div>
                }
              </div>
            }

            @if (selectedSeats().length > 1) {
              <mat-checkbox formControlName="sameForAll" class="same-for-all">
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
              <input matInput formControlName="remarks">
            </mat-form-field>

            <button mat-raised-button color="primary" type="submit"
              [disabled]="bookingForm.invalid || selectedSeats().length === 0 || loading()">
              {{ loading() ? 'Processing...' : 'Confirm Booking' }}
            </button>
          </form>
        </div>
      </div>

      <div class="seat-map-section" *ngIf="seats().length > 0">
        <h3>Select Seats</h3>
        <div class="bus-layout">
          <div class="seat-grid" [style.gridTemplateColumns]="getGridTemplateColumns()">
            @for (seat of seats(); track seat.seatId) {
              <div class="seat-cell"
                   [class.driver-seat]="seat.isDriver"
                   [class.sold]="seat.isSold"
                   [class.out-of-service]="!seat.isInService"
                   [class.selected]="isSelected(seat)"
                   [class.current]="seat.seatId === lastSelectedSeatId()"
                   [class.male]="seat.isSold && seat.passengerGender === 'Male'"
                   [class.female]="seat.isSold && seat.passengerGender === 'Female'"
                   [style.grid-row]="getVisualRow(seat)"
                   [style.grid-column]="getVisualCol(seat)"
                   (click)="toggleSeat(seat)">
                @if (seat.isDriver) {
                  <span class="driver-icon">&#x1F69A;</span>
                } @else if (seat.isSold && seat.passengerName) {
                  <span class="passenger-info">
                    <span class="passenger-initials">{{ getInitials(seat.passengerName) }}</span>
                    <span class="gender-icon">{{ seat.passengerGender === 'Male' ? '&#x2642;' : seat.passengerGender === 'Female' ? '&#x2640;' : '' }}</span>
                  </span>
                } @else {
                  <span class="seat-label">{{ seat.seatNumber }}</span>
                }
              </div>
            }
          </div>
          <div class="seat-legend">
            <span class="legend-item"><span class="legend-box available"></span> Available</span>
            <span class="legend-item"><span class="legend-box selected"></span> Selected</span>
            <span class="legend-item"><span class="legend-box sold"></span> Sold</span>
            <span class="legend-item"><span class="legend-box out-of-service"></span> Out of service</span>
            <span class="legend-item"><span class="legend-box driver"></span> Driver</span>
            <span class="legend-item"><span class="legend-box male"></span> Male</span>
            <span class="legend-item"><span class="legend-box female"></span> Female</span>
          </div>
        </div>
      </div>
    </div>

    <ng-template #noTrip>
      <div class="error-state">
        <p>Trip not found. Please search and select a trip.</p>
        <a mat-raised-button color="primary" routerLink="/search">Search Trips</a>
      </div>
    </ng-template>
  `,
  styles: [`
    .booking-container { max-width: 1200px; margin: 0 auto; padding: 2rem; }
    .booking-layout { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; }
    .trip-summary h2, .booking-form h3 { color: #333; margin-top: 0; }
    .summary-card { background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
    .summary-row { display: flex; justify-content: space-between; padding: 0.75rem 0; border-bottom: 1px solid #f0f0f0; }
    .summary-row:last-child { border-bottom: none; }
    .summary-row.total { font-size: 1.1rem; font-weight: 600; }
    .summary-row.total .price { color: #1a73e8; }
    .label { color: #666; }
    .value { color: #333; font-weight: 500; }
    .booking-form { background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
    .full-width { width: 100%; margin-bottom: 1rem; }
    .selected-seats { margin: 1.5rem 0; }
    .selected-seats h4 { margin: 0 0 0.75rem; color: #333; }
    .seat-chips { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    .seat-chip { background: #e3f2fd; color: #1a73e8; padding: 0.25rem 0.75rem; border-radius: 4px; font-weight: 600; }
    .error-state { text-align: center; padding: 4rem 2rem; }
    .error-state p { color: #666; margin-bottom: 1.5rem; }

    .passenger-card { margin-bottom: 1rem; padding: 1rem; border: 1px solid #e0e0e0; border-radius: 8px; background: #fafafa; }
    .passenger-card h4 { margin: 0 0 0.75rem; color: #333; font-size: 0.95rem; }
    .same-for-all { margin: 0.5rem 0 1rem; }
    .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }

    .seat-map-section { margin-top: 2rem; }
    .seat-map-section h3 { color: #333; margin-bottom: 1rem; }
    .bus-layout { background: #f5f5f5; padding: 1.5rem; border-radius: 8px; display: inline-block; }

    .seat-grid {
      display: grid;
      gap: 0;
      justify-content: center;
      margin-bottom: 1rem;
    }
    .seat-cell {
      width: 44px; height: 44px; border-radius: 6px; display: flex; flex-direction: column; align-items: center; justify-content: center;
      font-size: 0.7rem; font-weight: 600; cursor: pointer; transition: all 0.2s;
      background: #fff; border: 2px solid #e0e0e0; color: #333;
    }
    .seat-cell:hover:not(.sold):not(.out-of-service):not(.driver-seat) { border-color: #1a73e8; transform: scale(1.05); }
    .seat-cell.selected { background: #1a73e8; color: #fff; border-color: #1a73e8; }
    .seat-cell.current { box-shadow: 0 0 0 3px rgba(26, 115, 232, 0.35); }
    .seat-cell.sold { background: #ffebee; color: #c62828; border-color: #ffcdd2; cursor: not-allowed; }
    .seat-cell.sold.male { background: #e3f2fd; border-color: #90caf9; color: #1565c0; }
    .seat-cell.sold.female { background: #fce4ec; border-color: #f48fb1; color: #c2185b; }
    .seat-cell.out-of-service { background: #f5f5f5; color: #9e9e9e; border-color: #e0e0e0; cursor: not-allowed; text-decoration: line-through; }
    .seat-cell.driver-seat { background: #fff3e0; border-color: #ff9800; color: #e65100; cursor: default; }
    .driver-icon { font-size: 1.2rem; }
    .passenger-info { display: flex; flex-direction: column; align-items: center; line-height: 1.1; }
    .passenger-initials { font-size: 0.6rem; font-weight: 700; }
    .gender-icon { font-size: 0.55rem; }
    .seat-label { font-size: 0.75rem; }

    .seat-legend { margin-top: 1rem; display: flex; gap: 1.5rem; flex-wrap: wrap; }
    .legend-item { display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; color: #666; }
    .legend-box { width: 16px; height: 16px; border-radius: 4px; border: 2px solid #e0e0e0; }
    .legend-box.available { background: #fff; }
    .legend-box.selected { background: #1a73e8; border-color: #1a73e8; }
    .legend-box.sold { background: #ffebee; border-color: #ffcdd2; }
    .legend-box.out-of-service { background: #f5f5f5; }
    .legend-box.driver { background: #fff3e0; border-color: #ff9800; }
    .legend-box.male { background: #e3f2fd; border-color: #90caf9; }
    .legend-box.female { background: #fce4ec; border-color: #f48fb1; }

    @media (max-width: 768px) {
      .booking-layout { grid-template-columns: 1fr; }
      .seat-grid { grid-template-columns: repeat(8, 1fr); }
      .seat-cell { width: 32px; height: 32px; font-size: 0.65rem; }
    }
  `]
})
export class BookingComponent implements OnInit {
  trip = signal<TripDto | null>(null);
  seats = signal<SeatAvailabilityDto[]>([]);
  selectedSeats = signal<SeatAvailabilityDto[]>([]);
  travelDate = signal<string>('');
  loading = signal(false);
  loadingSeats = signal(false);
  error = signal<string | null>(null);
  lastSelectedSeatId = signal<string | null>(null);

  bookingForm: FormGroup;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private tripsService: TripsService,
    private bookingService: BookingService,
    private toast: ToastService,
    private snackBar: MatSnackBar
  ) {
    this.bookingForm = this.fb.nonNullable.group({
      sameForAll: [true],
      passengers: this.fb.array([
        this.fb.nonNullable.group({
          name: ['', Validators.required],
          mobile: ['', [Validators.required, Validators.pattern('^[0-9]{0,11}$'), Validators.maxLength(11)]],
          gender: [''],
          age: [null as number | null],
          nid: [''],
        })
      ]),
      paymentMethod: [0, Validators.required],
      remarks: [''],
    });

    this.bookingForm.get('sameForAll')!.valueChanges.subscribe(() => this.syncPassengers());
  }

  get passengers(): FormArray {
    return this.bookingForm.controls['passengers'] as FormArray;
  }

  get sameForAll(): boolean {
    return this.bookingForm.controls['sameForAll'].value;
  }

  ngOnInit(): void {
    const tripId = this.route.snapshot.paramMap.get('tripId');
    const dateParam = this.route.snapshot.queryParamMap.get('date');
    const travelDateStr = dateParam ? new Date(dateParam).toISOString().split('T')[0] : new Date().toISOString().split('T')[0];

    if (tripId) {
      this.travelDate.set(travelDateStr);
      this.loadTrip(tripId, travelDateStr);
    }
  }

  private loadTrip(scheduleId: string, travelDate: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.tripsService.getTripsForDate(travelDate).subscribe({
      next: (trips) => {
        const found = trips.find(t => t.scheduleId === scheduleId);
        if (found) {
          this.trip.set(found);
          this.loadSeats(scheduleId, travelDate);
        } else {
          this.error.set('Trip not found for the selected date.');
          this.loading.set(false);
        }
      },
      error: () => {
        this.error.set('Failed to load trip details.');
        this.loading.set(false);
      },
    });
  }

  private loadSeats(scheduleId: string, travelDate: string): void {
    this.loadingSeats.set(true);
    this.bookingService.getAvailableSeats(scheduleId, travelDate).subscribe({
      next: (seats) => {
        this.seats.set(seats);
        this.loadingSeats.set(false);
        this.loading.set(false);
      },
      error: () => {
        this.toast.error('Failed to load seat map.');
        this.loadingSeats.set(false);
        this.loading.set(false);
      },
    });
  }

  toggleSeat(seat: SeatAvailabilityDto): void {
    if (seat.isSold || !seat.isInService) return;

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

  getGridTemplateColumns(): string {
    const seats = this.seats();
    if (!seats.length) return 'repeat(12, 1fr)';
    const maxCol = Math.max(...seats.map(s => s.visualCol ?? 1));
    return `repeat(${maxCol}, 44px)`;
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
    return `${SeatClassLabel[seat.class]}${seat.isInService ? '' : ' · Out of service'}`;
  }

  onSubmit(): void {
    if (this.bookingForm.invalid || this.selectedSeats().length === 0 || !this.trip()) return;

    this.loading.set(true);
    const value = this.bookingForm.getRawValue();
    const trip = this.trip()!;
    const travelDate = this.travelDate();
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
          paymentMethod: value.paymentMethod,
          nidOrPassport: p.nid || undefined,
          gender: p.gender || undefined,
          age: p.age || undefined,
        };
      }),
      remarks: value.remarks || undefined,
    };

    this.bookingService.sellTickets(request).subscribe({
      next: (tickets) => {
        this.loading.set(false);
        const ticketNumbers = tickets.map(t => t.ticketNumber).join(', ');
        this.snackBar.open(`Booking confirmed! Tickets: ${ticketNumbers}`, 'Close', { duration: 5000 });
        this.router.navigate(['/my-tickets']);
      },
      error: (err) => {
        this.loading.set(false);
        const detail = err.error?.detail || err.error?.title || 'Could not complete booking. Please try again.';
        this.toast.error(detail);
        if (err.status === 409) {
          this.loadSeats(trip.scheduleId, travelDate);
          this.selectedSeats.set([]);
        }
      },
    });
  }
}
