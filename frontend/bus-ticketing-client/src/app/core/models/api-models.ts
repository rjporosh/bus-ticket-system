export interface TripDto {
  scheduleId: string;
  busId: string;
  busNumber: string;
  routeName: string;
  departureTime: string;
  arrivalTime: string;
  fareAmount: number;
  totalSeats: number;
  availableSeats: number;
}

export interface TripDetailDto extends TripDto {
  tripId: string;
  busName: string;
  busType: string;
  routeId: string;
  fromStationName: string;
  toStationName: string;
  travelDate: string;
  availableSeats: number;
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
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string;
}

export interface UserSummary {
  id: string;
  username: string;
  email: string;
  fullName: string;
  role: string;
  boothName?: string | null;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserSummary;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
