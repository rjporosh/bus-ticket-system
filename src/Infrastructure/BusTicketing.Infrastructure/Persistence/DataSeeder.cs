using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusTicketing.Infrastructure.Persistence;

/// <summary>
/// Idempotent startup seeder. Safe to run on every application start: every step
/// checks for existing data first, so re-running it never creates duplicates.
/// Seeds the exact scenario from the product brief: 2 roles, 3 users (admin +
/// one booth staff per booth), 2 stations, 2 routes, 6 buses (24 seats each),
/// and the 6 daily 7:00 AM schedules (3 per direction).
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IPasswordHasher passwordHasher, ILogger logger)
    {
        await db.Database.MigrateAsync();

        var adminRole = await GetOrCreateRoleAsync(db, SystemRoles.Admin, "Full administrative access.", isSystemRole: true);
        var boothRole = await GetOrCreateRoleAsync(db, SystemRoles.BoothStaff, "Ticket booth staff: sell, cancel and search tickets.", isSystemRole: true);
        await db.SaveChangesAsync();

        await GetOrCreateUserAsync(db, passwordHasher, "admin", "admin@bus-ticketing.local", "Admin@12345", "System Administrator", adminRole.Id, null);
        await GetOrCreateUserAsync(db, passwordHasher, "dhaka_staff_1", "dhaka.staff1@bus-ticketing.local", "Dhaka@12345", "Dhaka Booth Staff", boothRole.Id, "Dhaka");
        await GetOrCreateUserAsync(db, passwordHasher, "ctg_staff_1", "ctg.staff1@bus-ticketing.local", "Ctg@123456", "Chittagong Booth Staff", boothRole.Id, "Chittagong");
        await db.SaveChangesAsync();

        var dhaka = await GetOrCreateStationAsync(db, "Gabtoli Bus Terminal", "Dhaka");
        var chittagong = await GetOrCreateStationAsync(db, "Chittagong Central Terminal", "Chittagong");
        await db.SaveChangesAsync();

        var dhakaToCtg = await GetOrCreateRouteAsync(db, "Dhaka -> Chittagong", dhaka.Id, chittagong.Id, 264m, 360);
        var ctgToDhaka = await GetOrCreateRouteAsync(db, "Chittagong -> Dhaka", chittagong.Id, dhaka.Id, 264m, 360);
        await db.SaveChangesAsync();

        var buses = new List<Bus>();
        for (var i = 1; i <= 6; i++)
            buses.Add(await GetOrCreateBusAsync(db, $"Bus-{i}", $"DHK-METRO-{1000 + i}", "Green Line Paribahan"));
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var departure = new TimeOnly(7, 0);
        var arrival = new TimeOnly(13, 0);

        // Bus-1..3 run Dhaka -> Ctg, Bus-4..6 run Ctg -> Dhaka, matching the reference scenario.
        for (var i = 0; i < 3; i++)
            await GetOrCreateScheduleAsync(db, buses[i].Id, dhakaToCtg.Id, departure, arrival, today, 800m);

        for (var i = 3; i < 6; i++)
            await GetOrCreateScheduleAsync(db, buses[i].Id, ctgToDhaka.Id, departure, arrival, today, 800m);

        await db.SaveChangesAsync();

        logger.LogInformation("Database seed check complete.");
    }

    private static async Task<Role> GetOrCreateRoleAsync(ApplicationDbContext db, string name, string description, bool isSystemRole)
    {
        var existing = await db.Roles.FirstOrDefaultAsync(r => r.Name == name);
        if (existing is not null) return existing;

        var role = Role.Create(name, description, isSystemRole);
        db.Roles.Add(role);
        return role;
    }

    private static async Task<User> GetOrCreateUserAsync(
        ApplicationDbContext db, IPasswordHasher hasher, string username, string email, string password,
        string fullName, Guid roleId, string? boothName)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (existing is not null) return existing;

        var user = User.Create(username, email, hasher.Hash(password), fullName, roleId, boothName: boothName);
        db.Users.Add(user);
        return user;
    }

    private static async Task<Station> GetOrCreateStationAsync(ApplicationDbContext db, string name, string city)
    {
        var existing = await db.Stations.FirstOrDefaultAsync(s => s.Name == name && s.City == city);
        if (existing is not null) return existing;

        var station = Station.Create(name, city);
        db.Stations.Add(station);
        return station;
    }

    private static async Task<Domain.Entities.Route> GetOrCreateRouteAsync(
        ApplicationDbContext db, string name, Guid originId, Guid destinationId, decimal distanceKm, int durationMinutes)
    {
        var existing = await db.Routes.FirstOrDefaultAsync(r => r.OriginStationId == originId && r.DestinationStationId == destinationId);
        if (existing is not null) return existing;

        var route = Domain.Entities.Route.Create(name, originId, destinationId, distanceKm, durationMinutes);
        db.Routes.Add(route);
        return route;
    }

    private static async Task<Bus> GetOrCreateBusAsync(ApplicationDbContext db, string number, string registrationNumber, string operatorName)
    {
        var existing = await db.Buses.FirstOrDefaultAsync(b => b.Number == number);
        if (existing is not null) return existing;

        // 6 rows x 4 columns = 24 seats, matching the reference scenario exactly.
        var bus = Bus.Create(number, registrationNumber, operatorName, totalSeats: 24);
        var layout = SeatLayout.Generate(bus.Id, rows: 6, columns: 4, SeatClass.Economy);
        bus.AssignSeatLayout(layout);

        db.Buses.Add(bus);
        db.SeatLayouts.Add(layout);
        foreach (var seat in layout.Seats)
            db.Seats.Add(seat);

        return bus;
    }

    private static async Task GetOrCreateScheduleAsync(
        ApplicationDbContext db, Guid busId, Guid routeId, TimeOnly departure, TimeOnly arrival, DateOnly effectiveFrom, decimal fare)
    {
        var existing = await db.Schedules.FirstOrDefaultAsync(s => s.BusId == busId && s.RouteId == routeId && s.DepartureTime == departure);
        if (existing is not null) return;

        var schedule = Schedule.Create(busId, routeId, departure, arrival, DayOfWeekFlag.Daily, effectiveFrom, null, fare);
        db.Schedules.Add(schedule);
    }
}
