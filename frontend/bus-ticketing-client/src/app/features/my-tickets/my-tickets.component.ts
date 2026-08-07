import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/services/api.service';
import { TicketDto } from '../../core/models/api-models';

@Component({
  selector: 'app-my-tickets',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="my-tickets-container">
      <h2>My Tickets</h2>
      @if (loading()) {
        <div class="loading-row"><mat-spinner diameter="28" /></div>
      } @else {
        <div class="tickets-list" *ngIf="tickets().length > 0; else noTickets">
          <div class="ticket-card" *ngFor="let ticket of tickets()">
            <div class="ticket-header">
              <div class="ticket-number">{{ ticket.ticketNumber }}</div>
              <div class="ticket-status" [class]="'status-' + ticket.status.toLowerCase()">{{ ticket.status }}</div>
            </div>
            <div class="ticket-body">
              <div class="ticket-route">
                <span class="station">{{ ticket.routeName }}</span>
                <span class="arrow">→</span>
              </div>
              <div class="ticket-details">
                <div class="detail-row">
                  <span class="label">Bus:</span>
                  <span class="value">{{ ticket.busName }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Date:</span>
                  <span class="value">{{ ticket.travelDate | date:'fullDate' }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Departure:</span>
                  <span class="value">{{ ticket.departureTime }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Arrival:</span>
                  <span class="value">{{ ticket.arrivalTime }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Passenger:</span>
                  <span class="value">{{ ticket.passengerName }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Seats:</span>
                  <span class="value">{{ ticket.seatNumbers.join(', ') }}</span>
                </div>
                <div class="detail-row total">
                  <span class="label">Total Paid:</span>
                  <span class="value price">৳{{ ticket.totalFare | number:'1.2-2' }}</span>
                </div>
              </div>
            </div>
            <div class="ticket-footer" *ngIf="ticket.status === 'Confirmed'">
              <button mat-stroked-button color="warn" (click)="cancelTicket(ticket.id)">Cancel Ticket</button>
            </div>
          </div>
        </div>

        <ng-template #noTickets>
          <div class="empty-state">
            <p>You haven't booked any tickets yet.</p>
            <a mat-raised-button color="primary" routerLink="/search">Search & Book</a>
          </div>
        </ng-template>
      }
    </div>
  `,
  styles: [`
    .my-tickets-container { max-width: 900px; margin: 0 auto; padding: 2rem; }
    h2 { color: #333; margin-top: 0; }
    .loading-row { display: flex; justify-content: center; padding: 32px; }
    .tickets-list { display: flex; flex-direction: column; gap: 1.5rem; }
    .ticket-card { background: white; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); overflow: hidden; }
    .ticket-header { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.5rem; background: #f8f9fa; border-bottom: 1px solid #eee; }
    .ticket-number { font-weight: 600; color: #333; }
    .ticket-status { padding: 0.25rem 0.75rem; border-radius: 4px; font-size: 0.875rem; font-weight: 600; }
    .status-confirmed { background: #e8f5e9; color: #2e7d32; }
    .status-cancelled { background: #ffebee; color: #c62828; }
    .status-pending { background: #fff3e0; color: #ef6c00; }
    .ticket-body { padding: 1.5rem; }
    .ticket-route { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 1rem; font-size: 1.1rem; }
    .ticket-route .station { font-weight: 600; color: #333; }
    .ticket-route .arrow { color: #1a73e8; }
    .ticket-details { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 0.75rem; }
    .detail-row { display: flex; justify-content: space-between; padding: 0.5rem 0; border-bottom: 1px solid #f5f5f5; }
    .detail-row:last-child { border-bottom: none; }
    .detail-row.total { font-weight: 600; font-size: 1.05rem; }
    .detail-row.total .price { color: #1a73e8; }
    .label { color: #666; }
    .value { color: #333; }
    .ticket-footer { padding: 1rem 1.5rem; border-top: 1px solid #eee; display: flex; justify-content: flex-end; }
    .empty-state { text-align: center; padding: 4rem 2rem; }
    .empty-state p { color: #666; margin-bottom: 1.5rem; }
    @media (max-width: 768px) {
      .ticket-header { flex-direction: column; align-items: flex-start; gap: 0.5rem; }
    }
  `]
})
export class MyTicketsComponent implements OnInit {
  protected readonly tickets = signal<TicketDto[]>([]);
  protected readonly loading = signal(false);

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.api.get<any>('/booking/tickets', { pageSize: 50 }).subscribe({
      next: (result) => {
        const items = Array.isArray(result?.items) ? result.items : [];
        this.tickets.set(items.map((t: any) => ({
          ...t,
          status: t.status === 0 ? 'Sold' : t.status === 1 ? 'Cancelled' : 'Pending',
        })));
        this.loading.set(false);
      },
      error: () => {
        this.tickets.set([]);
        this.loading.set(false);
      },
    });
  }

  cancelTicket(ticketId: string): void {
    if (!confirm('Are you sure you want to cancel this ticket?')) return;
    this.api.post(`/booking/tickets/${ticketId}/cancel`, { reason: 'Cancelled by user' }).subscribe({
      next: () => {
        this.ngOnInit();
      },
      error: () => {
        alert('Could not cancel ticket. Please try again.');
      },
    });
  }
}