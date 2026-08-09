export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UserSummary {
  id: string;
  username: string;
  email: string;
  fullName: string;
  role: string;
  boothName: string | null;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserSummary;
}

export interface UserDto {
  id: string;
  username: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  boothName: string | null;
  isActive: boolean;
  roleId: string;
  roleName: string;
  createdAtUtc: string;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
}

export interface StationDto {
  id: string;
  name: string;
  city: string;
  address: string | null;
  isActive: boolean;
}

export interface RouteDto {
  id: string;
  name: string;
  originStationId: string;
  originStationName: string;
  destinationStationId: string;
  destinationStationName: string;
  distanceKm: number;
  estimatedDurationMinutes: number;
  isActive: boolean;
}

export interface BusDto {
  id: string;
  number: string;
  registrationNumber: string;
  operatorName: string;
  totalSeats: number;
  isActive: boolean;
  seatLayoutRows: number;
  seatLayoutColumns: number;
  seatLayoutType: number;
  seatLayoutConfig: string | null;
}

export type SeatClass = 0 | 1 | 2; // Economy | Business | Sleeper
export const SeatClassLabel: Record<SeatClass, string> = {
  0: 'Economy',
  1: 'Business',
  2: 'Sleeper',
};

export type LayoutType = 0 | 1; // StandardGrid | RealBus
export const LayoutTypeLabel: Record<LayoutType, string> = {
  0: 'Standard Grid',
  1: 'Real Bus',
};

export interface SeatDto {
  id: string;
  seatNumber: string;
  rowLabel: string;
  columnNumber: number;
  class: SeatClass;
  isActive: boolean;
  isDriver?: boolean;
  visualRow?: number;
  visualCol?: number;
}

export interface SeatLayoutDto {
  id: string;
  busId: string;
  busNumber: string;
  rows: number;
  columns: number;
  layoutType: number;
  layoutConfigJson: string | null;
  seats: SeatDto[];
}

// Bit flags matching BusTicketing.Domain.Enums.DayOfWeekFlag
export enum DayOfWeekFlag {
  None = 0,
  Monday = 1 << 0,
  Tuesday = 1 << 1,
  Wednesday = 1 << 2,
  Thursday = 1 << 3,
  Friday = 1 << 4,
  Saturday = 1 << 5,
  Sunday = 1 << 6,
  Daily = 127,
}

export const DayOfWeekOptions: { label: string; value: DayOfWeekFlag }[] = [
  { label: 'Mon', value: DayOfWeekFlag.Monday },
  { label: 'Tue', value: DayOfWeekFlag.Tuesday },
  { label: 'Wed', value: DayOfWeekFlag.Wednesday },
  { label: 'Thu', value: DayOfWeekFlag.Thursday },
  { label: 'Fri', value: DayOfWeekFlag.Friday },
  { label: 'Sat', value: DayOfWeekFlag.Saturday },
  { label: 'Sun', value: DayOfWeekFlag.Sunday },
];

export type ScheduleStatus = 0 | 1 | 2 | 3; // Scheduled | InProgress | Completed | Cancelled
export const ScheduleStatusLabel: Record<ScheduleStatus, string> = {
  0: 'Scheduled',
  1: 'In Progress',
  2: 'Completed',
  3: 'Cancelled',
};

export interface ScheduleDto {
  id: string;
  busId: string;
  busNumber: string;
  routeId: string;
  routeName: string;
  departureTime: string; // "HH:mm:ss"
  arrivalTime: string;
  daysOfWeek: DayOfWeekFlag;
  effectiveFrom: string; // "yyyy-MM-dd"
  effectiveTo: string | null;
  fareAmount: number;
  status: ScheduleStatus;
}

export interface TripDto {
  scheduleId: string;
  busId: string;
  busNumber: string;
  routeName: string;
  departureTime: string;
  arrivalTime: string;
  fareAmount: number;
  totalSeats: number;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

// --- Phase 2: Booking / Payment / Dashboard ---

export type TicketStatus = 0 | 1; // Sold | Cancelled
export const TicketStatusLabel: Record<TicketStatus, string> = {
  0: 'Sold',
  1: 'Cancelled',
};

export type PaymentStatus = 0 | 1 | 2 | 3; // Pending | Captured | Failed | Refunded
export const PaymentStatusLabel: Record<PaymentStatus, string> = {
  0: 'Pending',
  1: 'Captured',
  2: 'Failed',
  3: 'Refunded',
};

export type PaymentMethod = 0 | 1 | 2; // Cash | MockCard | MockMobileBanking
export const PaymentMethodLabel: Record<PaymentMethod, string> = {
  0: 'Cash',
  1: 'Mock Card',
  2: 'Mock Mobile Banking',
};

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

export enum TicketSearchField {
  TicketNumber = 0,
  MobileNumber = 1,
}

export interface RouteSalesDto {
  routeName: string;
  soldTickets: number;
  availableSeats: number;
  totalSales: number;
}

export interface BusSeatStatusDto {
  busNumber: string;
  routeName: string;
  departureTime: string;
  availableSeats: number;
  totalSeats: number;
}

export interface PaymentDto {
  id: string;
  ticketId: string;
  ticketNumber: string;
  passengerName: string;
  amount: number;
  method: PaymentMethod;
  status: PaymentStatus;
  transactionRef: string;
  processedAtUtc: string | null;
  failureReason: string | null;
}

export interface DashboardSummaryDto {
  date: string;
  totalSeats: number;
  soldSeats: number;
  availableSeats: number;
  totalSales: number;
  routeWiseSales: RouteSalesDto[];
  busWiseSeatStatus: BusSeatStatusDto[];
}
