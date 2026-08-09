# Phase 3 Progress & Build Error Fix Plan

## Current Progress
Phase 3 (Client-facing portal) is marked as delivered in `docs/ROADMAP.md`. The Angular client portal exists at `frontend/bus-ticketing-client/`.

### Build Status
- **Backend** (`BusTicketingSystem.sln`): Builds successfully with warnings only
- **Admin frontend** (`bus-ticketing-admin`): Builds successfully
- **Client frontend** (`bus-ticketing-client`): **BUILD FAILS**

## Build Errors in `bus-ticketing-client`

### Root Cause
The `TripDto` TypeScript interface in `frontend/bus-ticketing-client/src/app/core/models/api-models.ts` does not include several properties that `booking.component.ts` uses in its template and mock data.

### Specific Errors
1. `booking.component.ts:23` — `trip()!.fromStationName` / `trip()!.toStationName` not on `TripDto`
2. `booking.component.ts:27` — `trip()!.busName` / `trip()!.busType` not on `TripDto`
3. `booking.component.ts:31` — `trip()!.travelDate` not on `TripDto`
4. `booking.component.ts:135` — Mock object missing required `busNumber` property
5. `booking.component.ts:148` — `trip()!.tripId` not on `TripDto`

## Fix Plan

### 1. Update `TripDto` interface
**File**: `frontend/bus-ticketing-client/src/app/core/models/api-models.ts`

Add optional UI-specific properties to `TripDto`:
- `tripId?: string`
- `fromStationName?: string`
- `toStationName?: string`
- `busName?: string`
- `busType?: string`
- `travelDate?: string`
- `availableSeats?: number`

Keep existing required properties: `scheduleId`, `busId`, `busNumber`, `routeName`, `departureTime`, `arrivalTime`, `fareAmount`, `totalSeats`.

### 2. Fix mock data in `BookingComponent`
**File**: `frontend/bus-ticketing-client/src/app/features/booking/booking.component.ts`

Update the mock `TripDto` object on line ~135 to:
- Include required property `busNumber`
- Remove properties not in `TripDto` (e.g., `routeId`, `status`) or keep them with proper typing
- Ensure all template-referenced properties have values

### 3. Verify other components
- `search.component.ts` — uses only properties that already exist in `TripDto` (no changes needed)
- `home.component.ts` — declares `TripDto[]` but doesn't render it (no changes needed)

## Validation
Run `cd frontend/bus-ticketing-client && npm run build` and confirm zero errors.

## Additional Notes
- The backend `SellTicketCommand` expects `ScheduleId`, `SeatId`, and `TravelDate`, but the frontend `BookingRequest` currently uses `tripId` and `seatNumbers`. The booking submit is currently a mock (`TODO` comment), so this does not cause a build error yet, but should be aligned when the real API call is implemented.
- The `search.component.ts` router link passes `trip.scheduleId` to the booking route, but the route param is named `tripId` (`path: 'booking/:tripId'`). This is a naming inconsistency but not a build error.
