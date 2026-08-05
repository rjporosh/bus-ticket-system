import { Component, OnInit } from '@angular/core';
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
    @media (max-width: 768px) { .hero h1 { font-size: 1.75rem; } }
  `]
})
export class HomeComponent implements OnInit {
  featuredTrips: TripDto[] = [];

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    // Optionally load featured trips from API
  }
}