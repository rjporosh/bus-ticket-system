using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Schedules;

public record ScheduleDto(
    Guid Id,
    Guid BusId,
    string BusNumber,
    Guid RouteId,
    string RouteName,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    DayOfWeekFlag DaysOfWeek,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    decimal FareAmount,
    ScheduleStatus Status);

public record CreateScheduleCommand(
    Guid BusId,
    Guid RouteId,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    DayOfWeekFlag DaysOfWeek,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    decimal FareAmount) : IRequest<Result<ScheduleDto>>;

public class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.DaysOfWeek).NotEqual(DayOfWeekFlag.None);
        RuleFor(x => x.FareAmount).GreaterThan(0);
        RuleFor(x => x.ArrivalTime).NotEqual(x => x.DepartureTime)
            .WithMessage("Arrival time must differ from departure time.");
    }
}

public class CreateScheduleCommandHandler : IRequestHandler<CreateScheduleCommand, Result<ScheduleDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateScheduleCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ScheduleDto>> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        var bus = await _db.Buses.FirstOrDefaultAsync(b => b.Id == request.BusId, cancellationToken);
        if (bus is null)
            return Result.Failure<ScheduleDto>(Error.NotFound("Bus was not found."));
        if (!bus.IsActive)
            return Result.Failure<ScheduleDto>(Error.Conflict($"Bus \"{bus.Number}\" is inactive and cannot be scheduled."));

        var route = await _db.Routes.FirstOrDefaultAsync(r => r.Id == request.RouteId, cancellationToken);
        if (route is null)
            return Result.Failure<ScheduleDto>(Error.NotFound("Route was not found."));

        // Prevent the same bus from being double-booked on overlapping recurring days.
        var busAlreadyScheduled = await _db.Schedules.AnyAsync(s =>
            s.BusId == request.BusId &&
            s.Status != ScheduleStatus.Cancelled &&
            (s.DaysOfWeek & request.DaysOfWeek) != DayOfWeekFlag.None &&
            s.DepartureTime == request.DepartureTime,
            cancellationToken);

        if (busAlreadyScheduled)
            return Result.Failure<ScheduleDto>(Error.Conflict(
                $"Bus \"{bus.Number}\" already has a schedule at {request.DepartureTime:HH:mm} on an overlapping day."));

        var schedule = Domain.Entities.Schedule.Create(
            request.BusId, request.RouteId, request.DepartureTime, request.ArrivalTime,
            request.DaysOfWeek, request.EffectiveFrom, request.EffectiveTo, request.FareAmount);

        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ScheduleDto(
            schedule.Id, bus.Id, bus.Number, route.Id, route.Name,
            schedule.DepartureTime, schedule.ArrivalTime, schedule.DaysOfWeek,
            schedule.EffectiveFrom, schedule.EffectiveTo, schedule.FareAmount, schedule.Status));
    }
}

public record UpdateScheduleCommand(
    Guid Id,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    DayOfWeekFlag DaysOfWeek,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    decimal FareAmount) : IRequest<Result<ScheduleDto>>;

public class UpdateScheduleCommandValidator : AbstractValidator<UpdateScheduleCommand>
{
    public UpdateScheduleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DaysOfWeek).NotEqual(DayOfWeekFlag.None);
        RuleFor(x => x.FareAmount).GreaterThan(0);
    }
}

public class UpdateScheduleCommandHandler : IRequestHandler<UpdateScheduleCommand, Result<ScheduleDto>>
{
    private readonly IApplicationDbContext _db;
    public UpdateScheduleCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ScheduleDto>> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _db.Schedules.Include(s => s.Bus).Include(s => s.Route)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure<ScheduleDto>(Error.NotFound($"Schedule {request.Id} was not found."));

        schedule.Reschedule(request.DepartureTime, request.ArrivalTime);
        schedule.UpdateFare(request.FareAmount);
        schedule.UpdateRecurrence(request.DaysOfWeek, request.EffectiveFrom, request.EffectiveTo);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<ScheduleDto>(Error.Conflict("This schedule was modified by someone else. Please reload and try again."));
        }

        return Result.Success(new ScheduleDto(
            schedule.Id, schedule.BusId, schedule.Bus.Number, schedule.RouteId, schedule.Route.Name,
            schedule.DepartureTime, schedule.ArrivalTime, schedule.DaysOfWeek,
            schedule.EffectiveFrom, schedule.EffectiveTo, schedule.FareAmount, schedule.Status));
    }
}

public record SetScheduleStatusCommand(Guid Id, bool Cancel) : IRequest<Result>;

public class SetScheduleStatusCommandHandler : IRequestHandler<SetScheduleStatusCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public SetScheduleStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(SetScheduleStatusCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure(Error.NotFound($"Schedule {request.Id} was not found."));

        if (request.Cancel) schedule.Cancel(); else schedule.Reactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public record GetSchedulesQuery(
    Guid? BusId, Guid? RouteId, ScheduleStatus? Status, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<ScheduleDto>>;

public class GetSchedulesQueryHandler : IRequestHandler<GetSchedulesQuery, PaginatedList<ScheduleDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSchedulesQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<ScheduleDto>> Handle(GetSchedulesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Schedules.Include(s => s.Bus).Include(s => s.Route).AsQueryable();

        if (request.BusId.HasValue) query = query.Where(s => s.BusId == request.BusId.Value);
        if (request.RouteId.HasValue) query = query.Where(s => s.RouteId == request.RouteId.Value);
        if (request.Status.HasValue) query = query.Where(s => s.Status == request.Status.Value);

        var projected = query.OrderBy(s => s.DepartureTime).Select(s => new ScheduleDto(
            s.Id, s.BusId, s.Bus.Number, s.RouteId, s.Route.Name,
            s.DepartureTime, s.ArrivalTime, s.DaysOfWeek, s.EffectiveFrom, s.EffectiveTo, s.FareAmount, s.Status));

        return PaginatedList<ScheduleDto>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public record GetScheduleByIdQuery(Guid Id) : IRequest<Result<ScheduleDto>>;

public class GetScheduleByIdQueryHandler : IRequestHandler<GetScheduleByIdQuery, Result<ScheduleDto>>
{
    private readonly IApplicationDbContext _db;
    public GetScheduleByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ScheduleDto>> Handle(GetScheduleByIdQuery request, CancellationToken cancellationToken)
    {
        var schedule = await _db.Schedules.Include(s => s.Bus).Include(s => s.Route)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        return schedule is null
            ? Result.Failure<ScheduleDto>(Error.NotFound($"Schedule {request.Id} was not found."))
            : Result.Success(new ScheduleDto(
                schedule.Id, schedule.BusId, schedule.Bus.Number, schedule.RouteId, schedule.Route.Name,
                schedule.DepartureTime, schedule.ArrivalTime, schedule.DaysOfWeek,
                schedule.EffectiveFrom, schedule.EffectiveTo, schedule.FareAmount, schedule.Status));
    }
}

/// <summary>Resolves recurring schedules into concrete same-day trips for a given travel date, e.g. for the "Today's Trips" dashboard widget.</summary>
public record TripDto(Guid ScheduleId, Guid BusId, string BusNumber, string RouteName, TimeOnly DepartureTime, TimeOnly ArrivalTime, decimal FareAmount, int TotalSeats, int AvailableSeats);

public record GetTripsForDateQuery(DateOnly TravelDate, Guid? RouteId = null) : IRequest<List<TripDto>>;

public class GetTripsForDateQueryHandler : IRequestHandler<GetTripsForDateQuery, List<TripDto>>
{
    private readonly IApplicationDbContext _db;
    public GetTripsForDateQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<TripDto>> Handle(GetTripsForDateQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Schedules
            .Include(s => s.Bus)
            .Include(s => s.Route)
            .Where(s => s.Status != ScheduleStatus.Cancelled)
            .Where(s => s.EffectiveFrom <= request.TravelDate)
            .Where(s => s.EffectiveTo == null || s.EffectiveTo >= request.TravelDate);

        if (request.RouteId.HasValue)
            query = query.Where(s => s.RouteId == request.RouteId.Value);

        var candidates = await query.ToListAsync(cancellationToken);

        var todaysSchedules = candidates
            .Where(s => s.RunsOn(request.TravelDate))
            .OrderBy(s => s.DepartureTime)
            .ToList();

        var scheduleIds = todaysSchedules.Select(s => s.Id).ToHashSet();

        var soldCountByScheduleId = await _db.Tickets
            .Where(t => scheduleIds.Contains(t.ScheduleId) && t.TravelDate == request.TravelDate && t.Status == TicketStatus.Sold)
            .GroupBy(t => t.ScheduleId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        return todaysSchedules
            .Select(s => new TripDto(
                s.Id, s.BusId, s.Bus.Number, s.Route.Name, s.DepartureTime, s.ArrivalTime, s.FareAmount,
                s.Bus.TotalSeats, s.Bus.TotalSeats - soldCountByScheduleId.GetValueOrDefault(s.Id, 0)))
            .ToList();
    }
}
