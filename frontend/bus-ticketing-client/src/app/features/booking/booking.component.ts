import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../core/services/api.service';
import { TripDto, SeatDto, BookingRequest, TicketDto } from '../../core/models/api-models';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <div class="booking-container" *ngIf="trip(); else noTrip">
      <div class="booking-layout">
        <div class="trip-summary">
          <h2>Confirm Your Booking</h2>
          <div class="summary-card">
            <div class="summary-row">
              <span class="label">Route</span>
              <span class="value">{{ trip()!.fromStationName }} → {{ trip()!.toStationName }}</span>
            </div>
            <div class="summary-row">
              <span class="label">Bus</span>
              <span class="value">{{ trip()!.busName }} ({{ trip()!.busType }})</span>
            </div>
            <div class="summary-row">
              <span class="label">Date</span>
              <span class="value">{{ trip()!.travelDate | date:'fullDate' }}</span>
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
              <mat-label>Email (optional)</mat-label>
              <input matInput formControlName="passengerEmail" type="email" placeholder="your@email.com">
            </mat-form-field>

            <div class="selected-seats">
              <h4>Selected Seats ({{ selectedSeats().length }})</h4>
              <div class="seat-chips">
                <span class="seat-chip" *ngFor="let seat of selectedSeats()">{{ seat }}</span>
              </div>
            </div>

            <button mat-raised-button color="primary" type="submit" [disabled]="bookingForm.invalid || selectedSeats().length === 0 || loading()">
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
  selectedSeats = signal<string[]>([]);
  bookingForm: FormGroup;
  loading = signal(false);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private api: ApiService
  ) {
    this.bookingForm = this.fb.group({
      passengerName: ['', Validators.required],
      passengerMobile: ['', Validators.required],
      passengerEmail: ['']
    });
  }

  ngOnInit(): void {
    const tripId = this.route.snapshot.paramMap.get('tripId');
    if (tripId) {
      // In real app, fetch trip by ID from API
      this.trip.set({
        tripId, scheduleId: '1', busId: '1', busName: 'Green Line', busType: 'AC',
        routeId: '1', routeName: 'Dhaka-Chittagong', fromStationName: 'Dhaka', toStationName: 'Chittagong',
        departureTime: '08:00', arrivalTime: '14:00', travelDate: new Date().toISOString().split('T')[0],
        fareAmount: 1200, availableSeats: 15, totalSeats: 40, status: 'Active'
      } as TripDto);
    }
  }

  onSubmit(): void {
    if (this.bookingForm.invalid || this.selectedSeats().length === 0) return;
    this.loading.set(true);
    const request: BookingRequest = {
      tripId: this.trip()!.tripId,
      passengerName: this.bookingForm.value.passengerName,
      passengerMobile: this.bookingForm.value.passengerMobile,
      passengerEmail: this.bookingForm.value.passengerEmail,
      seatNumbers: this.selectedSeats(),
      totalFare: this.selectedSeats().length * this.trip()!.fareAmount
    };
    // TODO: Call api.post<BookingRequest, TicketDto>('/booking/tickets', request)
    setTimeout(() => {
      alert('Booking confirmed! Ticket: ' + Math.random().toString(36).substr(2, 9).toUpperCase());
      this.router.navigate(['/my-tickets']);
      this.loading.set(false);
    }, 1000);
  }
}