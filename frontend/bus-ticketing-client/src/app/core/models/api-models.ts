export interface TripDto {
  tripId: string;
  scheduleId: string;
  busId: string;
  busName: string;
  busType: string;
  routeId: string;
  routeName: string;
  fromStationName: string;
  toStationName: string;
  departureTime: string;
  arrivalTime: string;
  travelDate: string;
  fareAmount: number;
  availableSeats: number;
  totalSeats: number;
  status: string;
}

export interface StationDto {
  id: string;
  name: string;
  city: string;
  address: string;
}

export interface RouteDto {
  id: string;
  name: string;
  fromStationId: string;
  toStationId: string;
  distanceKm: number;
  estimatedDurationMinutes: number;
  fromStationName?: string;
  toStationName?: string;
}

export interface SeatDto {
  seatNumber: string;
  row: number;
  column: number;
  seatType: string;
  isAvailable: boolean;
}

export interface BookingRequest {
  tripId: string;
  passengerName: string;
  passengerMobile: string;
  passengerEmail?: string;
  seatNumbers: string[];
  totalFare: number;
}

export interface TicketDto {
  id: string;
  ticketNumber: string;
  tripId: string;
  passengerName: string;
  passengerMobile: string;
  seatNumbers: string[];
  totalFare: number;
  status: string;
  bookedAt: string;
  cancelledAt?: string;
  cancellationReason?: string;
  busName: string;
  routeName: string;
  departureTime: string;
  arrivalTime: string;
  travelDate: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: {
    id: string;
    email: string;
    fullName: string;
    roles: string[];
  };
}