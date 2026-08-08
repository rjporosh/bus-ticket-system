import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
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
    MatSnackBarModule,
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
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Full Name</mat-label>
              <input matInput formControlName="passengerName" placeholder="As per ID">
              <mat-error *ngIf="bookingForm.get('passengerName')?.hasError('required')">Name is required</mat-error>
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Mobile Number</mat-label>
              <input matInput formControlName="passengerMobile" placeholder="01XXXXXXXXX">
              <mat-error *ngIf="bookingForm.get('passengerMobile')?.hasError('required')">Mobile is required</mat-error>
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Gender (optional)</mat-label>
              <mat-select formControlName="gender">
                <mat-option value="Male">Male</mat-option>
                <mat-option value="Female">Female</mat-option>
                <mat-option value="Other">Other</mat-option>
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>NID / Passport (optional)</mat-label>
              <input matInput formControlName="nidOrPassport">
            </mat-form-field>
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
    </div>

    <ng-template #noTrip>
      <div class="error-state">
        <p>Trip not found. Please search and select a trip.</p>
        <a mat-raised-button color="primary" routerLink="/search">Search Trips</a>
      </div>
    </ng-template>
  `,
  styles: [`
    .booking-container { max-width: 1100px; margin: 0 auto; padding: 2rem; }
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
    @media (max-width: 768px) {
      .booking-layout { grid-template-columns: 1fr; }
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
      passengerName: ['', Validators.required],
      passengerMobile: ['', Validators.required],
      gender: [''],
      nidOrPassport: [''],
      paymentMethod: [0, Validators.required],
      remarks: [''],
    });
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
    } else {
      if (current.length >= 10) {
        this.toast.error('You can select up to 10 seats.');
        return;
      }
      current.push(seat);
    }
    this.selectedSeats.set([...current]);
  }

  isSelected(seat: SeatAvailabilityDto): boolean {
    return this.selectedSeats().some(s => s.seatId === seat.seatId);
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

    const request: SellTicketsRequest = {
      scheduleId: trip.scheduleId,
      travelDate,
      items: selected.map(seat => ({
        seatId: seat.seatId,
        passengerName: value.passengerName,
        mobileNumber: value.passengerMobile,
        fareAmount: trip.fareAmount,
        paymentMethod: value.paymentMethod,
        nidOrPassport: value.nidOrPassport || undefined,
        gender: value.gender || undefined,
      })),
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
