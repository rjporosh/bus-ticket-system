creation date: 09-08-26
last modified date: 09-08-26
last modified reason: initial seed data for roles, users, stations, routes, buses
modified by developer name: Prince
developer name : Prince
context: Phase 4 database artifact generation
api /cron job/service/method name: DataSeeder
db-provider-name : postgres
-------

-----

INSERT INTO "Roles" ("Id", "Name", "Description", "IsSystemRole")
VALUES
    (uuid_generate_v4(), 'Admin', 'Full system access', TRUE),
    (uuid_generate_v4(), 'BoothStaff', 'Ticket selling and search', TRUE),
    (uuid_generate_v4(), 'Customer', 'Self-service booking and ticket viewing', TRUE)
ON CONFLICT ("Name") DO NOTHING;

INSERT INTO "RolePermissions" ("RoleId", "Permission")
SELECT r."Id", p."Permission"
FROM "Roles" r
CROSS JOIN (
    VALUES
        (0), (1), (2), (3), (4), (5), (6), (7), (8), (9),
        (10), (11), (12), (13), (14), (15), (16), (17), (18), (19),
        (20), (21), (22), (23), (24), (25), (26), (27), (28), (29)
) AS p("Permission")
WHERE r."Name" IN ('Admin', 'BoothStaff', 'Customer')
ON CONFLICT ("RoleId", "Permission") DO NOTHING;

INSERT INTO "Stations" ("Id", "Name", "City", "Address", "IsActive")
VALUES
    (uuid_generate_v4(), 'Dhaka Terminal', 'Dhaka', 'Saydabad, Dhaka', TRUE),
    (uuid_generate_v4(), 'Chittagong Terminal', 'Chittagong', 'Sholashahar, Chittagong', TRUE),
    (uuid_generate_v4(), 'Rajshahi Terminal', 'Rajshahi', 'Rajshahi Bus Stand', TRUE),
    (uuid_generate_v4(), 'Khulna Terminal', 'Khulna', 'Khulna City Terminal', TRUE),
    (uuid_generate_v4(), 'Sylhet Terminal', 'Sylhet', 'Sylhet Bus Terminal', TRUE)
ON CONFLICT ("Name", "City") DO NOTHING;

INSERT INTO "Routes" ("Id", "Name", "OriginStationId", "DestinationStationId", "DistanceKm", "EstimatedDurationMinutes", "IsActive")
SELECT r."Id", r."Name", r."OriginId", r."DestId", r."Distance", r."Duration", TRUE
FROM (
    VALUES
        ('Dhaka-Chittagong', (SELECT "Id" FROM "Stations" WHERE "Name" = 'Dhaka Terminal' AND "City" = 'Dhaka'), (SELECT "Id" FROM "Stations" WHERE "Name" = 'Chittagong Terminal' AND "City" = 'Chittagong'), 240.00, 300),
        ('Dhaka-Rajshahi', (SELECT "Id" FROM "Stations" WHERE "Name" = 'Dhaka Terminal' AND "City" = 'Dhaka'), (SELECT "Id" FROM "Stations" WHERE "Name" = 'Rajshahi Terminal' AND "City" = 'Rajshahi'), 340.00, 420),
        ('Dhaka-Khulna', (SELECT "Id" FROM "Stations" WHERE "Name" = 'Dhaka Terminal' AND "City" = 'Dhaka'), (SELECT "Id" FROM "Stations" WHERE "Name" = 'Khulna Terminal' AND "City" = 'Khulna'), 450.00, 540),
        ('Chittagong-Sylhet', (SELECT "Id" FROM "Stations" WHERE "Name" = 'Chittagong Terminal' AND "City" = 'Chittagong'), (SELECT "Id" FROM "Stations" WHERE "Name" = 'Sylhet Terminal' AND "City" = 'Sylhet'), 420.00, 480),
        ('Rajshahi-Khulna', (SELECT "Id" FROM "Stations" WHERE "Name" = 'Rajshahi Terminal' AND "City" = 'Rajshahi'), (SELECT "Id" FROM "Stations" WHERE "Name" = 'Khulna Terminal' AND "City" = 'Khulna'), 280.00, 360)
) AS r("Name", "OriginId", "DestId", "Distance", "Duration")
ON CONFLICT ("OriginStationId", "DestinationStationId") DO NOTHING;

INSERT INTO "Buses" ("Id", "Number", "RegistrationNumber", "OperatorName", "TotalSeats", "IsActive")
VALUES
    (uuid_generate_v4(), 'BUS-001', 'DHK-CT-001', 'Green Line Paribahan', 40, TRUE),
    (uuid_generate_v4(), 'BUS-002', 'DHK-RA-002', 'Hanif Enterprise', 36, TRUE),
    (uuid_generate_v4(), 'BUS-003', 'DHK-KH-003', 'Shyamoli Express', 44, TRUE)
ON CONFLICT ("Number") DO NOTHING;

INSERT INTO "SeatLayouts" ("BusId", "Rows", "Columns")
SELECT b."Id", 10, 4 FROM "Buses" b WHERE b."Number" = 'BUS-001'
ON CONFLICT ("BusId") DO NOTHING;

INSERT INTO "SeatLayouts" ("BusId", "Rows", "Columns")
SELECT b."Id", 9, 4 FROM "Buses" b WHERE b."Number" = 'BUS-002'
ON CONFLICT ("BusId") DO NOTHING;

INSERT INTO "SeatLayouts" ("BusId", "Rows", "Columns")
SELECT b."Id", 11, 4 FROM "Buses" b WHERE b."Number" = 'BUS-003'
ON CONFLICT ("BusId") DO NOTHING;

INSERT INTO "Schedules" ("Id", "BusId", "RouteId", "DepartureTime", "ArrivalTime", "DaysOfWeek", "EffectiveFrom", "FareAmount", "Status")
SELECT uuid_generate_v4(), b."Id", r."Id", '08:00:00'::TIME, '13:00:00'::TIME, 127, CURRENT_DATE, 800.00, 0
FROM "Buses" b, "Routes" r
WHERE b."Number" = 'BUS-001' AND r."Name" = 'Dhaka-Chittagong'
ON CONFLICT DO NOTHING;

INSERT INTO "Schedules" ("Id", "BusId", "RouteId", "DepartureTime", "ArrivalTime", "DaysOfWeek", "EffectiveFrom", "FareAmount", "Status")
SELECT uuid_generate_v4(), b."Id", r."Id", '22:00:00'::TIME, '03:00:00'::TIME, 127, CURRENT_DATE, 600.00, 0
FROM "Buses" b, "Routes" r
WHERE b."Number" = 'BUS-002' AND r."Name" = 'Dhaka-Rajshahi'
ON CONFLICT DO NOTHING;

INSERT INTO "Schedules" ("Id", "BusId", "RouteId", "DepartureTime", "ArrivalTime", "DaysOfWeek", "EffectiveFrom", "FareAmount", "Status")
SELECT uuid_generate_v4(), b."Id", r."Id", '10:00:00'::TIME, '16:00:00'::TIME, 127, CURRENT_DATE, 950.00, 0
FROM "Buses" b, "Routes" r
WHERE b."Number" = 'BUS-003' AND r."Name" = 'Dhaka-Khulna'
ON CONFLICT DO NOTHING;
