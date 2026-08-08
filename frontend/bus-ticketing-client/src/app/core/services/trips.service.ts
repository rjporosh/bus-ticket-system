import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, QueryParams } from './api.service';
import { TripDto } from '../models/api-models';
import { API_ENDPOINTS } from '../config/api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripsService {
  constructor(private readonly api: ApiService) {}

  getTripsForDate(travelDate: string, routeId?: string): Observable<TripDto[]> {
    return this.api.get(API_ENDPOINTS.trips.forDate, { travelDate, routeId });
  }

  searchTrips(query: {
    travelDate: string;
    originStationId?: string;
    destinationStationId?: string;
    originStationName?: string;
    destinationStationName?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Observable<any> {
    return this.api.get(API_ENDPOINTS.trips.search, query);
  }
}
