creation date: 09-08-26
last modified date: 09-08-26
last modified reason: initial audit and data integrity triggers
modified by developer name: Prince
developer name : Prince
context: Phase 4 database artifact generation
api /cron job/service/method name: GlobalExceptionMiddleware,AuditLogService
db-provider-name : postgres
-------

-----

CREATE OR REPLACE FUNCTION trg_audit_ticket_changes()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'UPDATE' THEN
        INSERT INTO "AuditLogs" ("Action", "EntityName", "EntityId", "Details", "PerformedByUserId", "PerformedByUsername", "OccurredAtUtc")
        VALUES ('UPDATE', 'Ticket', NEW."Id"::TEXT,
                'Status changed from ' || OLD."Status" || ' to ' || NEW."Status" || ', Reason: ' || COALESCE(NEW."CancellationReason", 'N/A'),
                NEW."CancelledByUserId", NEW."CancelledByUserId"::TEXT, NOW());
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO "AuditLogs" ("Action", "EntityName", "EntityId", "Details", "PerformedByUserId", "PerformedByUsername", "OccurredAtUtc")
        VALUES ('INSERT', 'Ticket', NEW."Id"::TEXT,
                'Ticket sold: ' || NEW."TicketNumber" || ', Seat: ' || NEW."SeatId"::TEXT || ', Passenger: ' || NEW."PassengerName",
                NEW."SoldByUserId", NEW."SoldByUserId"::TEXT, NOW());
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$;

CREATE TRIGGER trg_ticket_audit
AFTER INSERT OR UPDATE ON "Tickets"
FOR EACH ROW EXECUTE FUNCTION trg_audit_ticket_changes();

CREATE OR REPLACE FUNCTION trg_log_payment_changes()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'UPDATE' THEN
        INSERT INTO "AuditLogs" ("Action", "EntityName", "EntityId", "Details", "PerformedByUserId", "PerformedByUsername", "OccurredAtUtc")
        VALUES ('UPDATE', 'Payment', NEW."Id"::TEXT,
                'Payment status changed from ' || OLD."Status" || ' to ' || NEW."Status",
                COALESCE(NEW."ProcessedAtUtc"::TIMESTAMP, NOW())::UUID, NEW."ProcessedAtUtc"::TEXT, NOW());
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$;

CREATE TRIGGER trg_payment_audit
AFTER UPDATE ON "Payments"
FOR EACH ROW EXECUTE FUNCTION trg_log_payment_changes();

CREATE OR REPLACE FUNCTION trg_set_modified_timestamp()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW."ModifiedAtUtc" := NOW();
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "AuditLogs"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Buses"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Roles"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Stations"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "TicketNumberCounters"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "SeatLayouts"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "RolePermissions"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Users"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Routes"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Seats"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "RefreshTokens"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Schedules"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Tickets"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();

CREATE TRIGGER trg_set_modified_timestamp
BEFORE UPDATE ON "Payments"
FOR EACH ROW EXECUTE FUNCTION trg_set_modified_timestamp();
