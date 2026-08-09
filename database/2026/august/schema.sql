creation date: 09-08-26
last modified date: 09-08-26
last modified reason: initial schema generation from EF Core model snapshot
modified by developer name: Prince
developer name : Prince
context: Phase 4 database artifact generation
api /cron job/service/method name: BusTicketing API
db-provider-name : postgres
-------

-----

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE "AuditLogs" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "Action" VARCHAR(50) NOT NULL,
    "EntityName" VARCHAR(100) NOT NULL,
    "EntityId" VARCHAR(100) NOT NULL,
    "Details" VARCHAR(2000) NULL,
    "PerformedByUserId" UUID NULL,
    "PerformedByUsername" VARCHAR(50) NOT NULL,
    "OccurredAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

CREATE TABLE "Buses" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "Number" VARCHAR(30) NOT NULL,
    "RegistrationNumber" VARCHAR(30) NOT NULL,
    "OperatorName" VARCHAR(150) NOT NULL,
    "TotalSeats" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Buses" PRIMARY KEY ("Id")
);

CREATE TABLE "Roles" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(50) NOT NULL,
    "Description" VARCHAR(250) NULL,
    "IsSystemRole" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
);

CREATE TABLE "Stations" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(150) NOT NULL,
    "City" VARCHAR(100) NOT NULL,
    "Address" VARCHAR(300) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Stations" PRIMARY KEY ("Id")
);

CREATE TABLE "TicketNumberCounters" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "CounterDate" DATE NOT NULL,
    "LastNumber" INTEGER NOT NULL DEFAULT 0,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_TicketNumberCounters" PRIMARY KEY ("Id")
);

CREATE TABLE "SeatLayouts" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "BusId" UUID NOT NULL,
    "Rows" INTEGER NOT NULL,
    "Columns" INTEGER NOT NULL,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_SeatLayouts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SeatLayouts_Buses_BusId" FOREIGN KEY ("BusId") REFERENCES "Buses" ("Id") ON DELETE CASCADE
);

CREATE TABLE "RolePermissions" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "RoleId" UUID NOT NULL,
    "Permission" INTEGER NOT NULL,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Users" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "Username" VARCHAR(50) NOT NULL,
    "Email" VARCHAR(150) NOT NULL,
    "PasswordHash" VARCHAR(500) NOT NULL,
    "FullName" VARCHAR(120) NOT NULL,
    "PhoneNumber" VARCHAR(30) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "BoothName" VARCHAR(50) NULL,
    "RoleId" UUID NOT NULL,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Routes" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(150) NOT NULL,
    "OriginStationId" UUID NOT NULL,
    "DestinationStationId" UUID NOT NULL,
    "DistanceKm" NUMERIC(9,2) NOT NULL,
    "EstimatedDurationMinutes" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Routes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Routes_Stations_DestinationStationId" FOREIGN KEY ("DestinationStationId") REFERENCES "Stations" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Routes_Stations_OriginStationId" FOREIGN KEY ("OriginStationId") REFERENCES "Stations" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Seats" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "SeatLayoutId" UUID NOT NULL,
    "SeatNumber" VARCHAR(10) NOT NULL,
    "RowLabel" VARCHAR(2) NOT NULL,
    "ColumnNumber" INTEGER NOT NULL,
    "Class" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Seats" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Seats_SeatLayouts_SeatLayoutId" FOREIGN KEY ("SeatLayoutId") REFERENCES "SeatLayouts" ("Id") ON DELETE CASCADE
);

CREATE TABLE "RefreshTokens" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "UserId" UUID NOT NULL,
    "Token" VARCHAR(500) NOT NULL,
    "ExpiresAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL,
    "RevokedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ReplacedByToken" VARCHAR(500) NULL,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Schedules" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "BusId" UUID NOT NULL,
    "RouteId" UUID NOT NULL,
    "DepartureTime" TIME NOT NULL,
    "ArrivalTime" TIME NOT NULL,
    "DaysOfWeek" INTEGER NOT NULL,
    "EffectiveFrom" DATE NOT NULL,
    "EffectiveTo" DATE NULL,
    "FareAmount" NUMERIC(9,2) NOT NULL,
    "Status" INTEGER NOT NULL DEFAULT 0,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Schedules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Schedules_Buses_BusId" FOREIGN KEY ("BusId") REFERENCES "Buses" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Schedules_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Tickets" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "TicketNumber" VARCHAR(30) NOT NULL,
    "ScheduleId" UUID NOT NULL,
    "SeatId" UUID NOT NULL,
    "TravelDate" DATE NOT NULL,
    "PassengerName" VARCHAR(150) NOT NULL,
    "MobileNumber" VARCHAR(20) NOT NULL,
    "NidOrPassport" VARCHAR(50) NULL,
    "Gender" VARCHAR(20) NULL,
    "Remarks" VARCHAR(500) NULL,
    "FareAmount" NUMERIC(9,2) NOT NULL,
    "Status" INTEGER NOT NULL DEFAULT 0,
    "SoldByUserId" UUID NOT NULL,
    "SoldAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CancelledAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "CancellationReason" VARCHAR(500) NULL,
    "CancelledByUserId" UUID NULL,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Tickets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Tickets_Schedules_ScheduleId" FOREIGN KEY ("ScheduleId") REFERENCES "Schedules" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Tickets_Seats_SeatId" FOREIGN KEY ("SeatId") REFERENCES "Seats" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Payments" (
    "Id" UUID NOT NULL DEFAULT uuid_generate_v4(),
    "TicketId" UUID NOT NULL,
    "Amount" NUMERIC(9,2) NOT NULL,
    "Method" INTEGER NOT NULL DEFAULT 0,
    "Status" INTEGER NOT NULL DEFAULT 0,
    "TransactionRef" VARCHAR(50) NOT NULL,
    "ProcessedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "FailureReason" VARCHAR(500) NULL,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedBy" TEXT NULL,
    "ModifiedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ModifiedBy" TEXT NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAtUtc" TIMESTAMP WITH TIME ZONE NULL,
    "DeletedBy" TEXT NULL,
    "ConcurrencyStamp" UUID NOT NULL DEFAULT uuid_generate_v4(),
    CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Payments_Tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES "Tickets" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_AuditLogs_EntityName_EntityId" ON "AuditLogs" ("EntityName", "EntityId");
CREATE INDEX "IX_AuditLogs_OccurredAtUtc" ON "AuditLogs" ("OccurredAtUtc");
CREATE UNIQUE INDEX "IX_Buses_Number" ON "Buses" ("Number");
CREATE UNIQUE INDEX "IX_Buses_RegistrationNumber" ON "Buses" ("RegistrationNumber");
CREATE UNIQUE INDEX "IX_Payments_TicketId" ON "Payments" ("TicketId");
CREATE UNIQUE INDEX "IX_Payments_TransactionRef" ON "Payments" ("TransactionRef");
CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");
CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
CREATE UNIQUE INDEX "IX_RolePermissions_RoleId_Permission" ON "RolePermissions" ("RoleId", "Permission");
CREATE UNIQUE INDEX "IX_Roles_Name" ON "Roles" ("Name");
CREATE INDEX "IX_Routes_DestinationStationId" ON "Routes" ("DestinationStationId");
CREATE UNIQUE INDEX "IX_Routes_OriginStationId_DestinationStationId" ON "Routes" ("OriginStationId", "DestinationStationId");
CREATE INDEX "IX_Schedules_BusId_DepartureTime" ON "Schedules" ("BusId", "DepartureTime");
CREATE INDEX "IX_Schedules_RouteId" ON "Schedules" ("RouteId");
CREATE UNIQUE INDEX "IX_SeatLayouts_BusId" ON "SeatLayouts" ("BusId");
CREATE UNIQUE INDEX "IX_Seats_SeatLayoutId_SeatNumber" ON "Seats" ("SeatLayoutId", "SeatNumber");
CREATE UNIQUE INDEX "IX_Stations_Name_City" ON "Stations" ("Name", "City");
CREATE UNIQUE INDEX "IX_TicketNumberCounters_CounterDate" ON "TicketNumberCounters" ("CounterDate");
CREATE INDEX "IX_Tickets_MobileNumber" ON "Tickets" ("MobileNumber");
CREATE UNIQUE INDEX "IX_Tickets_ScheduleId_TravelDate_SeatId" ON "Tickets" ("ScheduleId", "TravelDate", "SeatId") WHERE "Status" = 0;
CREATE INDEX "IX_Tickets_SeatId" ON "Tickets" ("SeatId");
CREATE UNIQUE INDEX "IX_Tickets_TicketNumber" ON "Tickets" ("TicketNumber");
CREATE INDEX "IX_Tickets_TravelDate" ON "Tickets" ("TravelDate");
CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX "IX_Users_RoleId" ON "Users" ("RoleId");
CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
