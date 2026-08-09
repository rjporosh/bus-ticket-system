creation date: 09-08-26
last modified date: 09-08-26
last modified reason: initial reporting views for dashboard and trip discovery
modified by developer name: Prince
developer name : Prince
context: Phase 4 database artifact generation
api /cron job/service/method name: DashboardController,TripsService
db-provider-name : postgres
-------

-----

CREATE OR REPLACE VIEW vw_available_trips AS
SELECT
    sc."Id" AS "ScheduleId",
    sc."DepartureTime",
    sc."ArrivalTime",
    sc."FareAmount",
    sc."EffectiveFrom",
    sc."EffectiveTo",
    r."Name" AS "RouteName",
    r."DistanceKm",
    r."EstimatedDurationMinutes",
    b."Number" AS "BusNumber",
    b."TotalSeats",
    COUNT(s."Id") AS "TotalSeatsCount",
    COUNT(CASE WHEN t."Id" IS NULL THEN 1 END) AS "AvailableSeats"
FROM "Schedules" sc
JOIN "Routes" r ON sc."RouteId" = r."Id"
JOIN "Buses" b ON sc."BusId" = b."Id"
JOIN "SeatLayouts" sl ON sl."BusId" = b."Id"
JOIN "Seats" s ON s."SeatLayoutId" = sl."Id"
LEFT JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."TravelDate" = CURRENT_DATE AND t."Status" = 0 AND t."IsDeleted" = FALSE
WHERE sc."IsDeleted" = FALSE AND r."IsDeleted" = FALSE AND b."IsDeleted" = FALSE
GROUP BY sc."Id", r."Name", r."DistanceKm", r."EstimatedDurationMinutes", b."Number", b."TotalSeats";

CREATE OR REPLACE VIEW vw_sold_tickets AS
SELECT
    t."Id",
    t."TicketNumber",
    t."TravelDate",
    t."PassengerName",
    t."MobileNumber",
    t."FareAmount",
    t."Status",
    t."SoldAtUtc",
    sc."DepartureTime",
    r."Name" AS "RouteName",
    b."Number" AS "BusNumber",
    s."SeatNumber"
FROM "Tickets" t
JOIN "Schedules" sc ON t."ScheduleId" = sc."Id"
JOIN "Routes" r ON sc."RouteId" = r."Id"
JOIN "Buses" b ON sc."BusId" = b."Id"
JOIN "Seats" s ON t."SeatId" = s."Id"
WHERE t."IsDeleted" = FALSE;

CREATE OR REPLACE VIEW vw_dashboard_summary AS
SELECT
    CURRENT_DATE AS "ReportDate",
    COUNT(s."Id") AS "TotalSeats",
    COUNT(CASE WHEN t."Id" IS NOT NULL THEN 1 END) AS "SoldSeats",
    COUNT(CASE WHEN t."Id" IS NULL THEN 1 END) AS "AvailableSeats",
    COALESCE(SUM(t."FareAmount"), 0) AS "TotalSales"
FROM "Seats" s
JOIN "SeatLayouts" sl ON s."SeatLayoutId" = sl."Id"
JOIN "Schedules" sc ON sl."BusId" = sc."BusId"
LEFT JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."TravelDate" = CURRENT_DATE AND t."Status" = 0 AND t."IsDeleted" = FALSE
WHERE sc."EffectiveFrom" <= CURRENT_DATE AND (sc."EffectiveTo" IS NULL OR sc."EffectiveTo" >= CURRENT_DATE)
  AND sc."IsDeleted" = FALSE;

CREATE OR REPLACE VIEW vw_bus_seat_status AS
SELECT
    b."Id" AS "BusId",
    b."Number" AS "BusNumber",
    r."Name" AS "RouteName",
    sc."DepartureTime",
    COUNT(s."Id") AS "TotalSeats",
    COUNT(CASE WHEN t."Id" IS NOT NULL THEN 1 END) AS "SoldSeats",
    COUNT(CASE WHEN t."Id" IS NULL THEN 1 END) AS "AvailableSeats"
FROM "Buses" b
JOIN "Schedules" sc ON sc."BusId" = b."Id"
JOIN "Routes" r ON sc."RouteId" = r."Id"
JOIN "SeatLayouts" sl ON sl."BusId" = b."Id"
JOIN "Seats" s ON s."SeatLayoutId" = sl."Id"
LEFT JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."TravelDate" = CURRENT_DATE AND t."Status" = 0 AND t."IsDeleted" = FALSE
WHERE sc."EffectiveFrom" <= CURRENT_DATE AND (sc."EffectiveTo" IS NULL OR sc."EffectiveTo" >= CURRENT_DATE)
  AND sc."IsDeleted" = FALSE
GROUP BY b."Id", b."Number", r."Name", sc."DepartureTime";

CREATE OR REPLACE VIEW vw_route_sales AS
SELECT
    r."Id" AS "RouteId",
    r."Name" AS "RouteName",
    COUNT(t."Id") AS "SoldTickets",
    COALESCE(SUM(t."FareAmount"), 0) AS "TotalSales"
FROM "Routes" r
JOIN "Schedules" sc ON sc."RouteId" = r."Id"
JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."Status" = 0 AND t."IsDeleted" = FALSE
WHERE r."IsDeleted" = FALSE
GROUP BY r."Id", r."Name";
