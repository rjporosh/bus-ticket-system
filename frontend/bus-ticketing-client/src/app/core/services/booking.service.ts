import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, QueryParams } from './api.service';
import {
  SeatAvailabilityDto,
  TicketDto,
  PaginatedList,
  SellTicketRequest,
  SellTicketsRequest,
} from '../models/api-models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private readonly api: ApiService) {}

  getAvailableSeats(scheduleId: string, travelDate: string): Observable<SeatAvailabilityDto[]> {
    return this.api.get(`/booking/schedules/${scheduleId}/seats`, { travelDate });
  }

  sellTicket(body: SellTicketRequest): Observable<TicketDto> {
    return this.api.post('/booking/tickets', body);
  }

  sellTickets(body: SellTicketsRequest): Observable<TicketDto[]> {
    return this.api.post('/booking/tickets/batch', body);
  }

  cancelTicket(ticketId: string, reason: string): Observable<TicketDto> {
    return this.api.post(`/booking/tickets/${ticketId}/cancel`, { reason });
  }

  search(query: QueryParams): Observable<PaginatedList<TicketDto>> {
    return this.api.get('/booking/tickets', query);
  }
}
