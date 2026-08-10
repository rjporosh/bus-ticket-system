import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, QueryParams } from './api.service';
import { TicketDto, PaginatedList } from '../models/api-models';
import { API_ENDPOINTS } from '../config/api-endpoints';

export interface TicketQrCodeResponse {
  ticketId: string;
  ticketNumber: string;
  qrCodeBase64: string;
  verificationPayload: string;
}

@Injectable({ providedIn: 'root' })
export class TicketsService {
  constructor(private readonly api: ApiService) {}

  getMyTickets(pageNumber = 1, pageSize = 20): Observable<PaginatedList<TicketDto>> {
    return this.api.get(API_ENDPOINTS.tickets.myTickets, { pageNumber, pageSize });
  }

  cancel(ticketId: string, reason: string): Observable<TicketDto> {
    return this.api.post(API_ENDPOINTS.tickets.cancel(ticketId), { reason });
  }

  getQrCode(ticketId: string): Observable<TicketQrCodeResponse> {
    return this.api.get(API_ENDPOINTS.tickets.qrCode(ticketId));
  }

  printTicket(ticketId: string): Observable<Blob> {
    return this.api.getBlob(API_ENDPOINTS.tickets.print(ticketId));
  }
}
