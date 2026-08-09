export type TicketStatus = 0 | 1;
export const TicketStatusLabel: Record<TicketStatus, string> = {
  0: 'Sold',
  1: 'Cancelled',
};

export type PaymentStatus = 0 | 1 | 2 | 3;
export const PaymentStatusLabel: Record<PaymentStatus, string> = {
  0: 'Pending',
  1: 'Captured',
  2: 'Failed',
  3: 'Refunded',
};

export type PaymentMethod = 0 | 1 | 2;
export const PaymentMethodLabel: Record<PaymentMethod, string> = {
  0: 'Cash',
  1: 'Mock Card',
  2: 'Mock Mobile Banking',
};

export type SeatClass = 0 | 1 | 2;
export const SeatClassLabel: Record<SeatClass, string> = {
  0: 'Economy',
  1: 'Business',
  2: 'Sleeper',
};

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

export interface SeatAvailabilityDto {
  seatId: string;
  seatNumber: string;
  rowLabel: string;
  columnNumber: number;
  class: SeatClass;
  isInService: boolean;
  isSold: boolean;
  isDriver?: boolean;
  visualRow?: number;
  visualCol?: number;
  passengerName?: string;
  passengerGender?: string;
}

export interface TicketDto {
  id: string;
  ticketNumber: string;
  scheduleId: string;
  busNumber: string;
  routeName: string;
  seatId: string;
  seatNumber: string;
  travelDate: string;
  departureTime: string;
  passengerName: string;
  mobileNumber: string;
  nidOrPassport: string | null;
  gender: string | null;
  age: number | null;
  remarks: string | null;
  fareAmount: number;
  status: TicketStatus;
  soldByUsername: string;
  soldAtUtc: string;
  cancellationReason: string | null;
  cancelledAtUtc: string | null;
  paymentStatus: PaymentStatus | null;
  paymentTransactionRef: string | null;
}

export interface SellTicketRequest {
  scheduleId: string;
  seatId: string;
  travelDate: string;
  passengerName: string;
  mobileNumber: string;
  fareAmount: number;
  paymentMethod: PaymentMethod;
  nidOrPassport?: string;
  gender?: string;
  age?: number;
  remarks?: string;
}

export interface SellTicketsRequest {
  scheduleId: string;
  travelDate: string;
  items: SellTicketItem[];
  remarks?: string;
}

export interface SellTicketItem {
  seatId: string;
  passengerName: string;
  mobileNumber: string;
  fareAmount: number;
  paymentMethod: PaymentMethod;
  nidOrPassport?: string;
  gender?: string;
  age?: number;
}

export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
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
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
