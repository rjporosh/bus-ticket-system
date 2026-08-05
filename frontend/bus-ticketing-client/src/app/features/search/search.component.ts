import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../core/services/api.service';
import { TripDto } from '../../core/models/api-models';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <div class="search-container">
      <div class="search-card">
        <h2>Find Your Trip</h2>
        <form [formGroup]="searchForm" (ngSubmit)="onSearch()">
          <div class="form-row">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>From</mat-label>
              <input matInput formControlName="fromStation" placeholder="Departure city">
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>To</mat-label>
              <input matInput formControlName="toStation" placeholder="Destination city">
            </mat-form-field>
          </div>
          <div class="form-row">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Travel Date</mat-label>
              <input matInput formControlName="travelDate" type="date">
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Passengers</mat-label>
              <input matInput formControlName="passengers" type="number" min="1" max="10" value="1">
            </mat-form-field>
          </div>
          <button mat-raised-button color="primary" type="submit" [disabled]="searchForm.invalid || loading()">
            {{ loading() ? 'Searching...' : 'Search Trips' }}
          </button>
        </form>
      </div>

      <div class="results-section" *ngIf="trips().length > 0">
        <h3>Available Trips ({{ trips().length }})</h3>
        <div class="trip-list">
          <div class="trip-card" *ngFor="let trip of trips()">
            <div class="trip-header">
              <div class="route-info">
                <span class="station">{{ trip.fromStationName }}</span>
                <span class="arrow">→</span>
                <span class="station">{{ trip.toStationName }}</span>
              </div>
              <div class="trip-date">{{ trip.travelDate | date:'mediumDate' }}</div>
            </div>
            <div class="trip-body">
              <div class="trip-detail">
                <span class="label">Bus</span>
                <span class="value">{{ trip.busName }} ({{ trip.busType }})</span>
              </div>
              <div class="trip-detail">
                <span class="label">Departure</span>
                <span class="value">{{ trip.departureTime }}</span>
              </div>
              <div class="trip-detail">
                <span class="label">Arrival</span>
                <span class="value">{{ trip.arrivalTime }}</span>
              </div>
              <div class="trip-detail">
                <span class="label">Fare</span>
                <span class="value price">৳{{ trip.fareAmount | number:'1.2-2' }}</span>
              </div>
            </div>
            <div class="trip-footer">
              <span class="seats-info">{{ trip.availableSeats }} seats available</span>
              <button mat-raised-button color="accent" [routerLink]="['/booking', trip.tripId]">Select Seats</button>
            </div>
          </div>
        </div>
      </div>

      <div class="no-results" *ngIf="searched() && trips().length === 0">
        <p>No trips found for your criteria. Try different dates or routes.</p>
      </div>
    </div>
  `,
  styles: [`
    .search-container { max-width: 1000px; margin: 0 auto; padding: 2rem; }
    .search-card { background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); margin-bottom: 2rem; }
    .search-card h2 { margin-top: 0; color: #333; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1rem; }
    .full-width { width: 100%; }
    .results-section h3 { color: #333; margin-bottom: 1rem; }
    .trip-list { display: flex; flex-direction: column; gap: 1rem; }
    .trip-card { background: white; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); padding: 1.5rem; transition: transform 0.2s; }
    .trip-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.12); }
    .trip-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; padding-bottom: 1rem; border-bottom: 1px solid #eee; }
    .route-info { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; }
    .station { font-weight: 600; color: #333; }
    .arrow { color: #1a73e8; font-size: 1.25rem; }
    .trip-date { color: #666; }
    .trip-body { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 1rem; margin-bottom: 1rem; }
    .trip-detail { display: flex; flex-direction: column; }
    .label { font-size: 0.875rem; color: #666; margin-bottom: 0.25rem; }
    .value { font-weight: 500; color: #333; }
    .value.price { color: #1a73e8; font-size: 1.1rem; font-weight: 600; }
    .trip-footer { display: flex; justify-content: space-between; align-items: center; padding-top: 1rem; border-top: 1px solid #eee; }
    .seats-info { color: #666; }
    .no-results { text-align: center; padding: 3rem; color: #666; }
    @media (max-width: 768px) {
      .form-row { grid-template-columns: 1fr; }
      .trip-header { flex-direction: column; align-items: flex-start; gap: 0.5rem; }
      .trip-footer { flex-direction: column; gap: 1rem; align-items: stretch; }
      .trip-footer button { width: 100%; }
    }
  `]
})
export class SearchComponent implements OnInit {
  searchForm: FormGroup;
  trips = signal<TripDto[]>([]);
  searched = signal(false);
  loading = signal(false);

  constructor(private fb: FormBuilder, private api: ApiService, private router: Router) {
    this.searchForm = this.fb.group({
      fromStation: [''],
      toStation: [''],
      travelDate: ['', Validators.required],
      passengers: [1]
    });
  }

  ngOnInit(): void {}

  onSearch(): void {
    if (this.searchForm.invalid) return;
    this.loading.set(true);
    // TODO: Implement actual API call
    // Mock data for demo
    setTimeout(() => {
      this.trips.set([
        {
          tripId: '1', scheduleId: '1', busId: '1', busName: 'Green Line', busType: 'AC',
          routeId: '1', routeName: 'Dhaka-Chittagong', fromStationName: 'Dhaka', toStationName: 'Chittagong',
          departureTime: '08:00', arrivalTime: '14:00', travelDate: this.searchForm.value.travelDate,
          fareAmount: 1200, availableSeats: 15, totalSeats: 40, status: 'Active'
        },
        {
          tripId: '2', scheduleId: '2', busId: '2', busName: 'Shyamoli', busType: 'Non-AC',
          routeId: '1', routeName: 'Dhaka-Chittagong', fromStationName: 'Dhaka', toStationName: 'Chittagong',
          departureTime: '10:30', arrivalTime: '16:30', travelDate: this.searchForm.value.travelDate,
          fareAmount: 800, availableSeats: 8, totalSeats: 36, status: 'Active'
        }
      ]);
      this.searched.set(true);
      this.loading.set(false);
    }, 500);
  }
}