import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TicketsService, TicketQrCodeResponse } from '../../core/services/tickets.service';
import { ToastService } from '../../core/services/toast.service';
import { TicketDto, TicketStatusLabel } from '../../core/models/api-models';

@Component({
  selector: 'app-my-tickets',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatProgressSpinnerModule, MatSnackBarModule, MatDialogModule],
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
              <div class="ticket-status" [class]="'status-' + ticket.status">{{ statusLabel(ticket.status) }}</div>
            </div>
            <div class="ticket-body">
              <div class="ticket-route">
                <span class="station">{{ ticket.routeName }}</span>
                <span class="arrow">→</span>
              </div>
              <div class="ticket-details">
                <div class="detail-row">
                  <span class="label">Bus:</span>
                  <span class="value">{{ ticket.busNumber }}</span>
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
                  <span class="label">Passenger:</span>
                  <span class="value">{{ ticket.passengerName }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">Seat:</span>
                  <span class="value">{{ ticket.seatNumber }}</span>
                </div>
                <div class="detail-row total">
                  <span class="label">Total Paid:</span>
                  <span class="value price">৳{{ ticket.fareAmount | number:'1.2-2' }}</span>
                </div>
              </div>
            </div>
            <div class="ticket-footer" *ngIf="ticket.status === 0 && !isDeparturePast(ticket)">
              <button mat-stroked-button color="warn" (click)="cancelTicket(ticket)">Cancel Ticket</button>
              <button mat-stroked-button color="primary" (click)="showQrCode(ticket)">Show QR</button>
            </div>
            @if (isDeparturePast(ticket)) {
              <div class="ticket-footer">
                <span class="departed-note">Journey has departed — cancellation not allowed</span>
              </div>
            }
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

    @if (qrData()) {
      <div class="qr-overlay" (click)="closeQr()">
        <div class="qr-modal" (click)="$event.stopPropagation()">
          <h3>Ticket QR Code</h3>
          <p class="qr-ticket-number">{{ qrData()?.ticketNumber }}</p>
          <img [src]="'data:image/png;base64,' + qrData()?.qrCodeBase64" alt="QR Code" class="qr-image" />
          <p class="qr-hint">Show this at boarding</p>
          <button mat-raised-button color="primary" (click)="closeQr()">Close</button>
        </div>
      </div>
    }
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
    .status-0 { background: #e8f5e9; color: #2e7d32; }
    .status-1 { background: #ffebee; color: #c62828; }
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
    .ticket-footer { padding: 1rem 1.5rem; border-top: 1px solid #eee; display: flex; justify-content: flex-end; gap: 0.5rem; }
    .departed-note { color: #c62828; font-size: 0.875rem; font-weight: 600; }
    .empty-state { text-align: center; padding: 4rem 2rem; }
    .empty-state p { color: #666; margin-bottom: 1.5rem; }
    .qr-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.6); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .qr-modal { background: white; border-radius: 12px; padding: 2rem; text-align: center; max-width: 360px; width: 90%; }
    .qr-modal h3 { margin: 0 0 0.5rem; color: #333; }
    .qr-ticket-number { color: #666; margin: 0 0 1rem; font-weight: 600; }
    .qr-image { width: 200px; height: 200px; display: block; margin: 0 auto 1rem; }
    .qr-hint { color: #888; font-size: 0.875rem; margin: 0 0 1rem; }
    @media (max-width: 768px) {
      .ticket-header { flex-direction: column; align-items: flex-start; gap: 0.5rem; }
      .ticket-footer { flex-direction: column; }
    }
  `]
})
export class MyTicketsComponent implements OnInit {
  protected readonly tickets = signal<TicketDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly cancelling = signal<TicketDto | null>(null);
  protected readonly cancelReason = signal('');
  protected readonly qrData = signal<TicketQrCodeResponse | null>(null);

  constructor(
    private ticketsService: TicketsService,
    private toast: ToastService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.ticketsService.getMyTickets().subscribe({
      next: (result) => {
        this.tickets.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.tickets.set([]);
        this.loading.set(false);
      },
    });
  }

  statusLabel(status: TicketDto['status']): string {
    return TicketStatusLabel[status];
  }

  isDeparturePast(ticket: TicketDto): boolean {
    const now = new Date();
    const departure = new Date(`${ticket.travelDate}T${ticket.departureTime}`);
    return now > departure && ticket.status === 0;
  }

  showQrCode(ticket: TicketDto): void {
    this.ticketsService.getQrCode(ticket.id).subscribe({
      next: (data) => this.qrData.set(data),
      error: () => this.toast.error('Could not load QR code. Please try again.'),
    });
  }

  closeQr(): void {
    this.qrData.set(null);
  }

  cancelTicket(ticket: TicketDto): void {
    if (!confirm(`Are you sure you want to cancel ticket ${ticket.ticketNumber}?`)) return;

    this.ticketsService.cancel(ticket.id, 'Cancelled by user').subscribe({
      next: () => {
        this.toast.success(`Ticket ${ticket.ticketNumber} cancelled successfully.`);
        this.ngOnInit();
      },
      error: () => {
        this.toast.error('Could not cancel ticket. Please try again.');
      },
    });
  }
}
