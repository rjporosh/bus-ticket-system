creation date: 09-08-26
last modified date: 09-08-26
last modified reason: initial scalar and table functions for business logic
modified by developer name: Prince
developer name : Prince
context: Phase 4 database artifact generation
api /cron job/service/method name: BookingService,ScheduleService
db-provider-name : postgres
-------

-----

CREATE OR REPLACE FUNCTION fn_get_seat_availability(p_schedule_id UUID, p_travel_date DATE)
RETURNS TABLE (
    "Id" UUID,
    "SeatNumber" VARCHAR(10),
    "RowLabel" VARCHAR(2),
    "ColumnNumber" INTEGER,
    "Class" INTEGER,
    "IsActive" BOOLEAN,
    "IsSold" BOOLEAN
)
LANGUAGE sql
AS $$
    SELECT s."Id", s."SeatNumber", s."RowLabel", s."ColumnNumber", s."Class", s."IsActive",
           CASE WHEN t."Id" IS NOT NULL THEN TRUE ELSE FALSE END AS "IsSold"
    FROM "Seats" s
    JOIN "SeatLayouts" sl ON s."SeatLayoutId" = sl."Id"
    JOIN "Schedules" sc ON sl."BusId" = sc."BusId"
    LEFT JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."TravelDate" = p_travel_date AND t."SeatId" = s."Id" AND t."Status" = 0
    WHERE sc."Id" = p_schedule_id
    ORDER BY s."RowLabel", s."ColumnNumber";
$$;

CREATE OR REPLACE FUNCTION fn_calculate_fare(p_route_id UUID, p_distance_km NUMERIC)
RETURNS NUMERIC
LANGUAGE sql
AS $$
    SELECT p_distance_km * 2.5;
$$;

CREATE OR REPLACE FUNCTION fn_get_ticket_count_by_date(p_date DATE)
RETURNS INTEGER
LANGUAGE sql
AS $$
    SELECT COUNT(*) FROM "Tickets" WHERE "TravelDate" = p_date AND "Status" = 0 AND "IsDeleted" = FALSE;
$$;

CREATE OR REPLACE FUNCTION fn_is_seat_sold(p_schedule_id UUID, p_seat_id UUID, p_travel_date DATE)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    SELECT EXISTS (
        SELECT 1 FROM "Tickets"
        WHERE "ScheduleId" = p_schedule_id AND "SeatId" = p_seat_id AND "TravelDate" = p_travel_date AND "Status" = 0 AND "IsDeleted" = FALSE
    );
$$;

CREATE OR REPLACE FUNCTION fn_get_user_permissions(p_user_id UUID)
RETURNS TABLE ("Permission" INTEGER)
LANGUAGE sql
AS $$
    SELECT DISTINCT rp."Permission"
    FROM "Users" u
    JOIN "Roles" r ON u."RoleId" = r."Id"
    JOIN "RolePermissions" rp ON rp."RoleId" = r."Id"
    WHERE u."Id" = p_user_id AND u."IsDeleted" = FALSE AND r."IsDeleted" = FALSE AND rp."IsDeleted" = FALSE;
$$;
