using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BusTicketing.UnitTests.Domain;

public class ScheduleTests
{
    private static Schedule CreateDailySchedule(DateOnly effectiveFrom, DateOnly? effectiveTo = null) =>
        Schedule.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            new TimeOnly(7, 0), new TimeOnly(13, 0),
            DayOfWeekFlag.Daily, effectiveFrom, effectiveTo, 800m);

    [Fact]
    public void Create_WithNoDaysOfWeek_ThrowsDomainException()
    {
        var act = () => Schedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), new TimeOnly(7, 0), new TimeOnly(13, 0),
            DayOfWeekFlag.None, DateOnly.FromDateTime(DateTime.UtcNow), null, 800m);

        act.Should().Throw<DomainException>().WithMessage("*at least one day*");
    }

    [Fact]
    public void Create_WithZeroFare_ThrowsDomainException()
    {
        var act = () => Schedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), new TimeOnly(7, 0), new TimeOnly(13, 0),
            DayOfWeekFlag.Daily, DateOnly.FromDateTime(DateTime.UtcNow), null, 0m);

        act.Should().Throw<DomainException>().WithMessage("*Fare*");
    }

    [Fact]
    public void RunsOn_DailySchedule_RunsEveryDayFromEffectiveDate()
    {
        var effectiveFrom = new DateOnly(2026, 5, 1);
        var schedule = CreateDailySchedule(effectiveFrom);

        schedule.RunsOn(effectiveFrom).Should().BeTrue();
        schedule.RunsOn(effectiveFrom.AddDays(30)).Should().BeTrue();
    }

    [Fact]
    public void RunsOn_DateBeforeEffectiveFrom_ReturnsFalse()
    {
        var effectiveFrom = new DateOnly(2026, 5, 1);
        var schedule = CreateDailySchedule(effectiveFrom);

        schedule.RunsOn(effectiveFrom.AddDays(-1)).Should().BeFalse();
    }

    [Fact]
    public void RunsOn_DateAfterEffectiveTo_ReturnsFalse()
    {
        var effectiveFrom = new DateOnly(2026, 5, 1);
        var effectiveTo = new DateOnly(2026, 5, 10);
        var schedule = CreateDailySchedule(effectiveFrom, effectiveTo);

        schedule.RunsOn(effectiveTo.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void RunsOn_SpecificWeekdaysOnly_OnlyRunsOnFlaggedDays()
    {
        var schedule = Schedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), new TimeOnly(7, 0), new TimeOnly(13, 0),
            DayOfWeekFlag.Saturday | DayOfWeekFlag.Sunday,
            new DateOnly(2026, 5, 1), null, 800m);

        // 2026-05-02 is a Saturday, 2026-05-04 is a Monday.
        schedule.RunsOn(new DateOnly(2026, 5, 2)).Should().BeTrue();
        schedule.RunsOn(new DateOnly(2026, 5, 4)).Should().BeFalse();
    }

    [Fact]
    public void RunsOn_CancelledSchedule_AlwaysReturnsFalse()
    {
        var schedule = CreateDailySchedule(new DateOnly(2026, 5, 1));
        schedule.Cancel();

        schedule.RunsOn(new DateOnly(2026, 5, 5)).Should().BeFalse();
    }
}
