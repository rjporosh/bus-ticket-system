using BusTicketing.Domain.Common;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>
/// A recurring trip template: "Bus-1 runs the Dhaka -&gt; Ctg route, departing 7:00 AM,
/// on the days flagged in <see cref="DaysOfWeek"/>, within [EffectiveFrom, EffectiveTo]".
/// The Booking module (future phase) resolves a Schedule + a concrete travel date into
/// a bookable trip instance; this module only owns the recurring definition.
/// </summary>
public class Schedule : BaseEntity
{
    public Guid BusId { get; private set; }
    public Bus Bus { get; private set; } = default!;

    public Guid RouteId { get; private set; }
    public Route Route { get; private set; } = default!;

    public TimeOnly DepartureTime { get; private set; }
    public TimeOnly ArrivalTime { get; private set; }

    public DayOfWeekFlag DaysOfWeek { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public decimal FareAmount { get; private set; }
    public ScheduleStatus Status { get; private set; }

    private Schedule() { } // EF Core

    public static Schedule Create(
        Guid busId,
        Guid routeId,
        TimeOnly departureTime,
        TimeOnly arrivalTime,
        DayOfWeekFlag daysOfWeek,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        decimal fareAmount)
    {
        if (daysOfWeek == DayOfWeekFlag.None)
            throw new DomainException("Schedule must run on at least one day of the week.");
        if (fareAmount <= 0)
            throw new DomainException("Fare amount must be greater than zero.");
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Effective-to date cannot be before effective-from date.");

        return new Schedule
        {
            BusId = busId,
            RouteId = routeId,
            DepartureTime = departureTime,
            ArrivalTime = arrivalTime,
            DaysOfWeek = daysOfWeek,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            FareAmount = fareAmount,
            Status = ScheduleStatus.Scheduled
        };
    }

    public void Reschedule(TimeOnly departureTime, TimeOnly arrivalTime)
    {
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
    }

    public void UpdateFare(decimal fareAmount)
    {
        if (fareAmount <= 0)
            throw new DomainException("Fare amount must be greater than zero.");
        FareAmount = fareAmount;
    }

    public void UpdateRecurrence(DayOfWeekFlag daysOfWeek, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (daysOfWeek == DayOfWeekFlag.None)
            throw new DomainException("Schedule must run on at least one day of the week.");
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Effective-to date cannot be before effective-from date.");

        DaysOfWeek = daysOfWeek;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public void Cancel() => Status = ScheduleStatus.Cancelled;
    public void Reactivate() => Status = ScheduleStatus.Scheduled;

    /// <summary>True if this schedule runs on the given calendar date.</summary>
    public bool RunsOn(DateOnly date)
    {
        if (Status == ScheduleStatus.Cancelled) return false;
        if (date < EffectiveFrom) return false;
        if (EffectiveTo.HasValue && date > EffectiveTo.Value) return false;

        var flag = date.DayOfWeek switch
        {
            DayOfWeek.Monday => DayOfWeekFlag.Monday,
            DayOfWeek.Tuesday => DayOfWeekFlag.Tuesday,
            DayOfWeek.Wednesday => DayOfWeekFlag.Wednesday,
            DayOfWeek.Thursday => DayOfWeekFlag.Thursday,
            DayOfWeek.Friday => DayOfWeekFlag.Friday,
            DayOfWeek.Saturday => DayOfWeekFlag.Saturday,
            DayOfWeek.Sunday => DayOfWeekFlag.Sunday,
            _ => DayOfWeekFlag.None
        };

        return (DaysOfWeek & flag) == flag;
    }
}
