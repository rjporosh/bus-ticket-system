import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, QueryParams } from './api.service';
import {
  BusDto,
  DashboardSummaryDto,
  PaginatedList,
  RoleDto,
  RouteDto,
  ScheduleDto,
  SeatAvailabilityDto,
  SeatLayoutDto,
  StationDto,
  TicketDto,
  TripDto,
  UserDto,
  PaymentDto,
  PaymentStatus,
  PaymentMethod,
} from '../models/api-models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<UserDto>> {
    return this.api.get('/users', query);
  }
  getById(id: string): Observable<UserDto> {
    return this.api.get(`/users/${id}`);
  }
  create(body: unknown): Observable<UserDto> {
    return this.api.post('/users', body);
  }
  update(id: string, body: unknown): Observable<UserDto> {
    return this.api.put(`/users/${id}`, body);
  }
  setActive(id: string, isActive: boolean): Observable<void> {
    return this.api.patch(`/users/${id}/status`, { isActive });
  }
}

@Injectable({ providedIn: 'root' })
export class RolesService {
  constructor(private readonly api: ApiService) {}

  list(): Observable<RoleDto[]> {
    return this.api.get('/roles');
  }
  create(body: unknown): Observable<RoleDto> {
    return this.api.post('/roles', body);
  }
  update(id: string, body: unknown): Observable<RoleDto> {
    return this.api.put(`/roles/${id}`, body);
  }
}

@Injectable({ providedIn: 'root' })
export class StationsService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<StationDto>> {
    return this.api.get('/stations', query);
  }
  getById(id: string): Observable<StationDto> {
    return this.api.get(`/stations/${id}`);
  }
  create(body: unknown): Observable<StationDto> {
    return this.api.post('/stations', body);
  }
  update(id: string, body: unknown): Observable<StationDto> {
    return this.api.put(`/stations/${id}`, body);
  }
  setActive(id: string, isActive: boolean): Observable<void> {
    return this.api.patch(`/stations/${id}/status`, { isActive });
  }
}

@Injectable({ providedIn: 'root' })
export class RoutesService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<RouteDto>> {
    return this.api.get('/routes', query);
  }
  getById(id: string): Observable<RouteDto> {
    return this.api.get(`/routes/${id}`);
  }
  create(body: unknown): Observable<RouteDto> {
    return this.api.post('/routes', body);
  }
  update(id: string, body: unknown): Observable<RouteDto> {
    return this.api.put(`/routes/${id}`, body);
  }
  setActive(id: string, isActive: boolean): Observable<void> {
    return this.api.patch(`/routes/${id}/status`, { isActive });
  }
}

@Injectable({ providedIn: 'root' })
export class BusesService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<BusDto>> {
    return this.api.get('/buses', query);
  }
  getById(id: string): Observable<BusDto> {
    return this.api.get(`/buses/${id}`);
  }
  create(body: unknown): Observable<BusDto> {
    return this.api.post('/buses', body);
  }
  update(id: string, body: unknown): Observable<BusDto> {
    return this.api.put(`/buses/${id}`, body);
  }
  setActive(id: string, isActive: boolean): Observable<void> {
    return this.api.patch(`/buses/${id}/status`, { isActive });
  }
  getSeatLayout(busId: string): Observable<SeatLayoutDto> {
    return this.api.get(`/buses/${busId}/seat-layout`);
  }
  setSeatStatus(busId: string, seatId: string, isActive: boolean): Observable<void> {
    return this.api.patch(`/buses/${busId}/seat-layout/seats/${seatId}/status`, { isActive });
  }
  reclassifySeat(busId: string, seatId: string, seatClass: number): Observable<void> {
    return this.api.patch(`/buses/${busId}/seat-layout/seats/${seatId}/class`, { class: seatClass });
  }
}

@Injectable({ providedIn: 'root' })
export class SchedulesService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<ScheduleDto>> {
    return this.api.get('/schedules', query);
  }
  getById(id: string): Observable<ScheduleDto> {
    return this.api.get(`/schedules/${id}`);
  }
  create(body: unknown): Observable<ScheduleDto> {
    return this.api.post('/schedules', body);
  }
  update(id: string, body: unknown): Observable<ScheduleDto> {
    return this.api.put(`/schedules/${id}`, body);
  }
  setStatus(id: string, cancel: boolean): Observable<void> {
    return this.api.patch(`/schedules/${id}/status`, { cancel });
  }
  getTripsForDate(travelDate: string, routeId?: string): Observable<TripDto[]> {
    return this.api.get('/schedules/trips', { travelDate, routeId });
  }
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private readonly api: ApiService) {}

  getAvailableSeats(scheduleId: string, travelDate: string): Observable<SeatAvailabilityDto[]> {
    return this.api.get(`/booking/schedules/${scheduleId}/seats`, { travelDate });
  }
  sellTicket(body: unknown): Observable<TicketDto> {
    return this.api.post('/booking/tickets', body);
  }
  cancelTicket(ticketId: string, reason: string): Observable<TicketDto> {
    return this.api.post(`/booking/tickets/${ticketId}/cancel`, { reason });
  }
  search(query: QueryParams): Observable<PaginatedList<TicketDto>> {
    return this.api.get('/booking/tickets', query);
  }
  getPayments(query: QueryParams): Observable<PaginatedList<PaymentDto>> {
    return this.api.get('/payments', query);
  }
  capturePayment(paymentId: string): Observable<PaymentDto> {
    return this.api.post(`/payments/${paymentId}/capture`, {});
  }
  refundPayment(paymentId: string): Observable<PaymentDto> {
    return this.api.post(`/payments/${paymentId}/refund`, {});
  }
  failPayment(paymentId: string): Observable<PaymentDto> {
    return this.api.post(`/payments/${paymentId}/fail`, {});
  }
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly api: ApiService) {}

  getSummary(date: string): Observable<DashboardSummaryDto> {
    return this.api.get('/dashboard/summary', { date });
  }
}
