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
import { API_ENDPOINTS } from '../config/api-endpoints';

@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private readonly api: ApiService) {}

  getAvailableSeats(scheduleId: string, travelDate: string): Observable<SeatAvailabilityDto[]> {
    return this.api.get(API_ENDPOINTS.booking.availableSeats(scheduleId), { travelDate });
  }

  sellTicket(body: SellTicketRequest): Observable<TicketDto> {
    return this.api.post(API_ENDPOINTS.booking.sellTicket, body);
  }

  sellTickets(body: SellTicketsRequest): Observable<TicketDto[]> {
    return this.api.post(API_ENDPOINTS.booking.sellTickets, body);
  }

  cancelTicket(ticketId: string, reason: string): Observable<TicketDto> {
    return this.api.post(API_ENDPOINTS.booking.cancelTicket(ticketId), { reason });
  }

  search(query: QueryParams): Observable<PaginatedList<TicketDto>> {
    return this.api.get(API_ENDPOINTS.booking.search, query);
  }
}
