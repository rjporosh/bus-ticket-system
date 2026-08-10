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
import { API_ENDPOINTS } from '../config/api-endpoints';

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<UserDto>> {
    return this.api.get(API_ENDPOINTS.users.list, query);
  }
  getById(id: string): Observable<UserDto> {
    return this.api.get(API_ENDPOINTS.users.get(id));
  }
  create(body: unknown): Observable<UserDto> {
    return this.api.post(API_ENDPOINTS.users.create, body);
  }
  update(id: string, body: unknown): Observable<UserDto> {
    return this.api.put(API_ENDPOINTS.users.update(id), body);
  }
  setActive(id: string, isActive: boolean): Observable<void> {
    return this.api.patch(API_ENDPOINTS.users.setActive(id), { isActive });
  }
}

@Injectable({ providedIn: 'root' })
export class RolesService {
  constructor(private readonly api: ApiService) {}

  list(): Observable<RoleDto[]> {
    return this.api.get(API_ENDPOINTS.roles.list);
  }
  create(body: unknown): Observable<RoleDto> {
    return this.api.post(API_ENDPOINTS.roles.create, body);
  }
  update(id: string, body: unknown): Observable<RoleDto> {
    return this.api.put(API_ENDPOINTS.roles.update(id), body);
  }
}

@Injectable({ providedIn: 'root' })
export class StationsService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<StationDto>> {
    return this.api.get(API_ENDPOINTS.stations.list, query);
  }
  getById(id: string): Observable<StationDto> {
    return this.api.get(API_ENDPOINTS.stations.get(id));
  }
  create(body: unknown): Observable<StationDto> {
    return this.api.post(API_ENDPOINTS.stations.create, body);
  }
  update(id: string, body: unknown): Observable<StationDto> {
    return this.api.put(API_ENDPOINTS.stations.update(id), body);
  }
  setActive(id: string, isActive: boolean): Observable<void> {
    return this.api.patch(API_ENDPOINTS.stations.setActive(id), { isActive });
  }
}

@Injectable({ providedIn: 'root' })
export class RoutesService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<RouteDto>> {
    return this.api.get(API_ENDPOINTS.routes.list, query);
  }
  getById(id: string): Observable<RouteDto> {
    return this.api.get(API_ENDPOINTS.routes.get(id));
  }
  create(body: unknown): Observable<RouteDto> {
    return this.api.post(API_ENDPOINTS.routes.create, body);
  }
  update(id: string, body: unknown): Observable<RouteDto> {
    return this.api.put(API_ENDPOINTS.routes.update(id), body);
  }
  setActive(id: string, isActive: boolean): Observable<void> {
    return this.api.patch(API_ENDPOINTS.routes.setActive(id), { isActive });
  }
}

@Injectable({ providedIn: 'root' })
export class BusesService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<BusDto>> {
    return this.api.get(API_ENDPOINTS.buses.list, query);
  }
  getById(id: string): Observable<BusDto> {
    return this.api.get(API_ENDPOINTS.buses.get(id));
  }
  create(body: unknown): Observable<BusDto> {
    return this.api.post(API_ENDPOINTS.buses.create, body);
  }
  update(id: string, body: unknown): Observable<BusDto> {
    return this.api.put(API_ENDPOINTS.buses.update(id), body);
  }
  setActive(id: string, isActive: boolean): Observable<void> {
    return this.api.patch(API_ENDPOINTS.buses.setActive(id), { isActive });
  }
  getSeatLayout(busId: string): Observable<SeatLayoutDto> {
    return this.api.get(API_ENDPOINTS.buses.seatLayout(busId));
  }
  setSeatStatus(busId: string, seatId: string, isActive: boolean): Observable<void> {
    return this.api.patch(API_ENDPOINTS.buses.setSeatStatus(busId, seatId), { isActive });
  }
  reclassifySeat(busId: string, seatId: string, seatClass: number): Observable<void> {
    return this.api.patch(API_ENDPOINTS.buses.reclassifySeat(busId, seatId), { class: seatClass });
  }
}

@Injectable({ providedIn: 'root' })
export class SchedulesService {
  constructor(private readonly api: ApiService) {}

  list(query: QueryParams): Observable<PaginatedList<ScheduleDto>> {
    return this.api.get(API_ENDPOINTS.schedules.list, query);
  }
  getById(id: string): Observable<ScheduleDto> {
    return this.api.get(API_ENDPOINTS.schedules.get(id));
  }
  create(body: unknown): Observable<ScheduleDto> {
    return this.api.post(API_ENDPOINTS.schedules.create, body);
  }
  update(id: string, body: unknown): Observable<ScheduleDto> {
    return this.api.put(API_ENDPOINTS.schedules.update(id), body);
  }
  setStatus(id: string, cancel: boolean): Observable<void> {
    return this.api.patch(API_ENDPOINTS.schedules.setStatus(id), { cancel });
  }
  getTripsForDate(travelDate: string, routeId?: string): Observable<TripDto[]> {
    return this.api.get(API_ENDPOINTS.schedules.trips, { travelDate, routeId });
  }
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private readonly api: ApiService) {}

  getAvailableSeats(scheduleId: string, travelDate: string): Observable<SeatAvailabilityDto[]> {
    return this.api.get(API_ENDPOINTS.booking.availableSeats(scheduleId), { travelDate });
  }
  sellTicket(body: unknown): Observable<TicketDto> {
    return this.api.post(API_ENDPOINTS.booking.sellTicket, body);
  }
  sellTickets(body: unknown): Observable<TicketDto[]> {
    return this.api.post(API_ENDPOINTS.booking.sellTickets, body);
  }
  cancelTicket(ticketId: string, reason: string): Observable<TicketDto> {
    return this.api.post(API_ENDPOINTS.booking.cancelTicket(ticketId), { reason });
  }
  search(query: QueryParams): Observable<PaginatedList<TicketDto>> {
    return this.api.get(API_ENDPOINTS.booking.search, query);
  }
  printTicket(ticketId: string): Observable<Blob> {
    return this.api.getBlob(API_ENDPOINTS.booking.print(ticketId));
  }
  getPayments(query: QueryParams): Observable<PaginatedList<PaymentDto>> {
    return this.api.get(API_ENDPOINTS.payments.list, query);
  }
  capturePayment(paymentId: string): Observable<PaymentDto> {
    return this.api.post(API_ENDPOINTS.payments.capture(paymentId), {});
  }
  refundPayment(paymentId: string): Observable<PaymentDto> {
    return this.api.post(API_ENDPOINTS.payments.refund(paymentId), {});
  }
  failPayment(paymentId: string): Observable<PaymentDto> {
    return this.api.post(API_ENDPOINTS.payments.fail(paymentId), {});
  }
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly api: ApiService) {}

  getSummary(date: string): Observable<DashboardSummaryDto> {
    return this.api.get(API_ENDPOINTS.dashboard.summary, { date });
  }
}
