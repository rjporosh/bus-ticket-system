creation date: 09-08-26
last modified date: 09-08-26
last modified reason: initial stored procedures for booking and reporting
modified by developer name: Prince
developer name : Prince
context: Phase 4 database artifact generation
api /cron job/service/method name: BookingController,ScheduleController
db-provider-name : postgres
-------

-----

CREATE OR REPLACE PROCEDURE sp_sell_ticket(
    p_schedule_id UUID,
    p_travel_date DATE,
    p_seat_id UUID,
    p_passenger_name VARCHAR(150),
    p_mobile_number VARCHAR(20),
    p_fare_amount NUMERIC(9,2),
    p_payment_method INTEGER,
    p_user_id UUID,
    p_nid_or_passport VARCHAR(50) DEFAULT NULL,
    p_gender VARCHAR(20) DEFAULT NULL,
    p_remarks VARCHAR(500) DEFAULT NULL,
    OUT p_ticket_id UUID,
    OUT p_ticket_number VARCHAR(30),
    OUT p_payment_status INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_ticket_number VARCHAR(30);
    v_counter INTEGER;
    v_date_part VARCHAR(8);
BEGIN
    SELECT INTO v_date_part TO_CHAR(p_travel_date, 'YYYYMMDD');

    SELECT "LastNumber" INTO v_counter FROM "TicketNumberCounters" WHERE "CounterDate" = p_travel_date FOR UPDATE;
    IF v_counter IS NULL THEN
        INSERT INTO "TicketNumberCounters" ("CounterDate", "LastNumber") VALUES (p_travel_date, 1) RETURNING "LastNumber" INTO v_counter;
    ELSE
        UPDATE "TicketNumberCounters" SET "LastNumber" = "LastNumber" + 1 WHERE "CounterDate" = p_travel_date RETURNING "LastNumber" INTO v_counter;
    END IF;

    v_ticket_number := 'TKT-' || v_date_part || '-' || LPAD(v_counter::TEXT, 4, '0');

    INSERT INTO "Tickets" ("TicketNumber", "ScheduleId", "SeatId", "TravelDate", "PassengerName", "MobileNumber", "FareAmount", "Status", "SoldByUserId", "SoldAtUtc", "NidOrPassport", "Gender", "Remarks")
    VALUES (v_ticket_number, p_schedule_id, p_seat_id, p_travel_date, p_passenger_name, p_mobile_number, p_fare_amount, 0, p_user_id, NOW(), p_nid_or_passport, p_gender, p_remarks)
    RETURNING "Id" INTO p_ticket_id;

    INSERT INTO "Payments" ("TicketId", "Amount", "Method", "Status", "TransactionRef")
    VALUES (p_ticket_id, p_fare_amount, p_payment_method, 1, 'MOCK-' || v_ticket_number)
    RETURNING "Status" INTO p_payment_status;

    p_ticket_number := v_ticket_number;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_sell_tickets_batch(
    p_schedule_id UUID,
    p_travel_date DATE,
    p_items JSONB,
    p_user_id UUID,
    p_remarks VARCHAR(500) DEFAULT NULL,
    OUT p_ticket_ids UUID[],
    OUT p_ticket_numbers VARCHAR[],
    OUT p_status INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
    item JSONB;
    v_ticket_id UUID;
    v_ticket_number VARCHAR(30);
    v_payment_status INTEGER;
    v_index INTEGER := 1;
BEGIN
    p_ticket_ids := ARRAY[]::UUID[];
    p_ticket_numbers := ARRAY[]::VARCHAR[];
    p_status := 1;

    FOR item IN SELECT * FROM jsonb_array_elements(p_items)
    LOOP
        CALL sp_sell_ticket(
            p_schedule_id,
            p_travel_date,
            (item->>'seatId')::UUID,
            item->>'passengerName',
            item->>'mobileNumber',
            (item->>'fareAmount')::NUMERIC(9,2),
            (item->>'paymentMethod')::INTEGER,
            p_user_id,
            item->>'nidOrPassport',
            item->>'gender',
            p_remarks,
            v_ticket_id,
            v_ticket_number,
            v_payment_status
        );
        p_ticket_ids := array_append(p_ticket_ids, v_ticket_id);
        p_ticket_numbers := array_append(p_ticket_numbers, v_ticket_number);
        v_index := v_index + 1;
    END LOOP;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_cancel_ticket(
    p_ticket_id UUID,
    p_reason VARCHAR(500),
    p_user_id UUID,
    OUT p_status INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "Tickets"
    SET "Status" = 1,
        "CancelledAtUtc" = NOW(),
        "CancellationReason" = p_reason,
        "CancelledByUserId" = p_user_id,
        "ModifiedAtUtc" = NOW(),
        "ModifiedBy" = p_user_id::TEXT
    WHERE "Id" = p_ticket_id AND "Status" = 0;

    IF FOUND THEN
        UPDATE "Payments" SET "Status" = 3, "ProcessedAtUtc" = NOW() WHERE "TicketId" = p_ticket_id AND "Status" = 1;
        p_status := 1;
    ELSE
        p_status := 0;
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_get_available_seats(
    p_schedule_id UUID,
    p_travel_date DATE
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT s."Id", s."SeatNumber", s."RowLabel", s."ColumnNumber", s."Class", s."IsActive",
           CASE WHEN t."Id" IS NOT NULL THEN TRUE ELSE FALSE END AS "IsSold"
    FROM "Seats" s
    JOIN "SeatLayouts" sl ON s."SeatLayoutId" = sl."Id"
    JOIN "Schedules" sc ON sl."BusId" = sc."BusId"
    LEFT JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."TravelDate" = p_travel_date AND t."SeatId" = s."Id" AND t."Status" = 0
    WHERE sc."Id" = p_schedule_id
    ORDER BY s."RowLabel", s."ColumnNumber";
END;
$$;

CREATE OR REPLACE PROCEDURE sp_search_tickets(
    p_search_by INTEGER DEFAULT NULL,
    p_search_text VARCHAR(100) DEFAULT NULL,
    p_travel_date DATE DEFAULT NULL,
    p_route_id UUID DEFAULT NULL,
    p_status INTEGER DEFAULT NULL,
    p_page_number INTEGER DEFAULT 1,
    p_page_size INTEGER DEFAULT 20
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_offset INTEGER := (p_page_number - 1) * p_page_size;
BEGIN
    SELECT COUNT(*) INTO p_page_size FROM "Tickets" t
    JOIN "Schedules" sc ON t."ScheduleId" = sc."Id"
    JOIN "Routes" r ON sc."RouteId" = r."Id"
    WHERE (p_search_by IS NULL OR
           (p_search_by = 0 AND t."TicketNumber" ILIKE '%' || p_search_text || '%') OR
           (p_search_by = 1 AND t."MobileNumber" ILIKE '%' || p_search_text || '%') OR
           (p_search_by = 2 AND r."Id"::TEXT = p_search_text) OR
           (p_search_by = 3 AND t."Status"::TEXT = p_search_text))
      AND (p_travel_date IS NULL OR t."TravelDate" = p_travel_date)
      AND (p_route_id IS NULL OR r."Id" = p_route_id)
      AND (p_status IS NULL OR t."Status" = p_status)
      AND t."IsDeleted" = FALSE;

    SELECT json_agg(row_to_json(t)) INTO p_search_text FROM (
        SELECT t.*, sc."BusId", r."Name" AS "RouteName"
        FROM "Tickets" t
        JOIN "Schedules" sc ON t."ScheduleId" = sc."Id"
        JOIN "Routes" r ON sc."RouteId" = r."Id"
        WHERE (p_search_by IS NULL OR
               (p_search_by = 0 AND t."TicketNumber" ILIKE '%' || p_search_text || '%') OR
               (p_search_by = 1 AND t."MobileNumber" ILIKE '%' || p_search_text || '%') OR
               (p_search_by = 2 AND r."Id"::TEXT = p_search_text) OR
               (p_search_by = 3 AND t."Status"::TEXT = p_search_text))
          AND (p_travel_date IS NULL OR t."TravelDate" = p_travel_date)
          AND (p_route_id IS NULL OR r."Id" = p_route_id)
          AND (p_status IS NULL OR t."Status" = p_status)
          AND t."IsDeleted" = FALSE
        ORDER BY t."SoldAtUtc" DESC
        LIMIT p_page_size OFFSET v_offset
    ) t;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_get_dashboard_summary(
    p_target_date DATE
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT
        COUNT(*) AS "TotalSeats",
        COUNT(CASE WHEN t."Id" IS NOT NULL THEN 1 END) AS "SoldSeats",
        COUNT(CASE WHEN t."Id" IS NULL THEN 1 END) AS "AvailableSeats",
        COALESCE(SUM(t."FareAmount"), 0) AS "TotalSales"
    INTO p_target_date
    FROM "Seats" s
    JOIN "SeatLayouts" sl ON s."SeatLayoutId" = sl."Id"
    JOIN "Schedules" sc ON sl."BusId" = sc."BusId"
    LEFT JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."TravelDate" = p_target_date AND t."Status" = 0
    WHERE sc."EffectiveFrom" <= p_target_date AND (sc."EffectiveTo" IS NULL OR sc."EffectiveTo" >= p_target_date)
      AND sc."IsDeleted" = FALSE;

    SELECT json_agg(row_to_json(rs)) INTO p_target_date FROM (
        SELECT r."Name" AS "RouteName", COUNT(t."Id") AS "SoldTickets", COUNT(s."Id") - COUNT(t."Id") AS "AvailableSeats", COALESCE(SUM(t."FareAmount"), 0) AS "TotalSales"
        FROM "Routes" r
        JOIN "Schedules" sc ON sc."RouteId" = r."Id"
        JOIN "SeatLayouts" sl ON sl."BusId" = sc."BusId"
        JOIN "Seats" s ON s."SeatLayoutId" = sl."Id"
        LEFT JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."TravelDate" = p_target_date AND t."Status" = 0
        WHERE sc."EffectiveFrom" <= p_target_date AND (sc."EffectiveTo" IS NULL OR sc."EffectiveTo" >= p_target_date)
          AND sc."IsDeleted" = FALSE
        GROUP BY r."Name"
    ) rs;

    SELECT json_agg(row_to_json(bs)) INTO p_target_date FROM (
        SELECT b."Number" AS "BusNumber", r."Name" AS "RouteName", sc."DepartureTime",
               COUNT(s."Id") AS "TotalSeats", COUNT(CASE WHEN t."Id" IS NOT NULL THEN 1 END) AS "AvailableSeats"
        FROM "Buses" b
        JOIN "Schedules" sc ON sc."BusId" = b."Id"
        JOIN "Routes" r ON sc."RouteId" = r."Id"
        JOIN "SeatLayouts" sl ON sl."BusId" = b."Id"
        JOIN "Seats" s ON s."SeatLayoutId" = sl."Id"
        LEFT JOIN "Tickets" t ON t."ScheduleId" = sc."Id" AND t."TravelDate" = p_target_date AND t."Status" = 0
        WHERE sc."EffectiveFrom" <= p_target_date AND (sc."EffectiveTo" IS NULL OR sc."EffectiveTo" >= p_target_date)
          AND sc."IsDeleted" = FALSE
        GROUP BY b."Number", r."Name", sc."DepartureTime"
    ) bs;
END;
$$;
