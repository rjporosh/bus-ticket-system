import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { TripDto } from '../../core/models/api-models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="home-container">
      <section class="hero">
        <div class="hero-content">
          <h1>Plan Your Journey with BusTicketing</h1>
          <p>Book bus tickets online. Safe, fast, and convenient.</p>
          <a routerLink="/search" class="cta-button">Search Trips</a>
        </div>
      </section>

      <section class="features">
        <div class="feature-card">
          <div class="feature-icon">🔍</div>
          <h3>Easy Search</h3>
          <p>Find trips by route and date in seconds.</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon">💺</div>
          <h3>Choose Your Seat</h3>
          <p>Select the perfect seat from an interactive seat map.</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon">📱</div>
          <h3>Instant Booking</h3>
          <p>Book tickets and get confirmation immediately.</p>
        </div>
      </section>

      <section class="how-it-works">
        <h2>How It Works</h2>
        <div class="steps">
          <div class="step">
            <span class="step-number">1</span>
            <h4>Search</h4>
            <p>Enter your route and travel date.</p>
          </div>
          <div class="step">
            <span class="step-number">2</span>
            <h4>Select Seats</h4>
            <p>Pick seats from the available seat map.</p>
          </div>
          <div class="step">
            <span class="step-number">3</span>
            <h4>Enter Details</h4>
            <p>Provide passenger information.</p>
          </div>
          <div class="step">
            <span class="step-number">4</span>
            <h4>Confirm & Pay</h4>
            <p>Complete mock payment and receive ticket.</p>
          </div>
        </div>
      </section>

      <section class="featured-trips" *ngIf="featuredTrips().length > 0">
        <h2>Featured Trips Today</h2>
        <div class="trip-list">
          <div class="trip-card" *ngFor="let trip of featuredTrips()">
            <div class="trip-header">
              <div class="route-info">
                <span class="route">{{ trip.routeName }}</span>
              </div>
              <div class="trip-date">Today</div>
            </div>
            <div class="trip-body">
              <div class="trip-detail">
                <span class="label">Bus</span>
                <span class="value">{{ trip.busNumber }}</span>
              </div>
              <div class="trip-detail">
                <span class="label">Available Seats</span>
                <span class="value">{{ trip.availableSeats }} / {{ trip.totalSeats }}</span>
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
              <a mat-raised-button color="accent" [routerLink]="['/search']">View Details</a>
            </div>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .home-container { }
    .hero { background: linear-gradient(135deg, #1a73e8 0%, #0d47a1 100%); color: white; padding: 4rem 2rem; text-align: center; }
    .hero-content { max-width: 800px; margin: 0 auto; }
    .hero h1 { font-size: 2.5rem; margin-bottom: 1rem; font-weight: 700; }
    .hero p { font-size: 1.25rem; margin-bottom: 2rem; opacity: 0.9; }
    .cta-button { display: inline-block; background: white; color: #1a73e8; padding: 0.75rem 2rem; border-radius: 4px; text-decoration: none; font-weight: 600; transition: transform 0.2s, box-shadow 0.2s; }
    .cta-button:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.2); }
    .features { max-width: 1200px; margin: 3rem auto; padding: 0 2rem; display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 2rem; }
    .feature-card { background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); text-align: center; transition: transform 0.2s; }
    .feature-card:hover { transform: translateY(-4px); }
    .feature-icon { font-size: 2.5rem; margin-bottom: 1rem; }
    .feature-card h3 { margin-bottom: 0.5rem; color: #333; }
    .how-it-works { max-width: 1200px; margin: 4rem auto; padding: 0 2rem; }
    .how-it-works h2 { text-align: center; margin-bottom: 2rem; color: #333; }
    .steps { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 2rem; }
    .step { text-align: center; padding: 1.5rem; }
    .step-number { display: inline-flex; align-items: center; justify-content: center; width: 40px; height: 40px; background: #1a73e8; color: white; border-radius: 50%; font-weight: 700; margin-bottom: 1rem; }
    .step h4 { margin-bottom: 0.5rem; color: #333; }
    .featured-trips { max-width: 1000px; margin: 4rem auto; padding: 0 2rem; }
    .featured-trips h2 { text-align: center; margin-bottom: 2rem; color: #333; }
    .trip-list { display: flex; flex-direction: column; gap: 1rem; }
    .trip-card { background: white; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); padding: 1.5rem; }
    .trip-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; padding-bottom: 1rem; border-bottom: 1px solid #eee; }
    .route-info { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; }
    .route { font-weight: 600; color: #333; }
    .trip-date { color: #666; }
    .trip-body { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 1rem; margin-bottom: 1rem; }
    .trip-detail { display: flex; flex-direction: column; }
    .label { font-size: 0.875rem; color: #666; margin-bottom: 0.25rem; }
    .value { font-weight: 500; color: #333; }
    .value.price { color: #1a73e8; font-size: 1.1rem; font-weight: 600; }
    .trip-footer { display: flex; justify-content: flex-end; padding-top: 1rem; border-top: 1px solid #eee; }
    @media (max-width: 768px) {
      .hero h1 { font-size: 1.75rem; }
    }
  `]
})
export class HomeComponent implements OnInit {
  featuredTrips = signal<TripDto[]>([]);

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.api.get<TripDto[]>('/schedules/trips', { travelDate: today }).subscribe({
      next: (trips) => {
        this.featuredTrips.set(Array.isArray(trips) ? trips.slice(0, 5) : []);
      },
      error: () => {
        this.featuredTrips.set([]);
      }
    });
  }
}