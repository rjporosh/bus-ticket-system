import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, QueryParams } from './api.service';
import { TicketDto, PaginatedList } from '../models/api-models';

@Injectable({ providedIn: 'root' })
export class TicketsService {
  constructor(private readonly api: ApiService) {}

  getMyTickets(pageNumber = 1, pageSize = 20): Observable<PaginatedList<TicketDto>> {
    return this.api.get('/booking/my-tickets', { pageNumber, pageSize });
  }

  cancel(ticketId: string, reason: string): Observable<TicketDto> {
    return this.api.post(`/booking/tickets/${ticketId}/cancel`, { reason });
  }
}
