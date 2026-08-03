using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BusTicketing.UnitTests.Domain;

public class TicketTests
{
    private static Ticket SellTicket() => Ticket.Sell(
        "TKT-20260802-0001", Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 2),
        "Rahim Uddin", "01700000000", 800m, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void Sell_WithValidData_CreatesSoldTicket()
    {
        var ticket = SellTicket();

        ticket.Status.Should().Be(TicketStatus.Sold);
        ticket.TicketNumber.Should().Be("TKT-20260802-0001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Sell_WithoutPassengerName_ThrowsDomainException(string name)
    {
        var act = () => Ticket.Sell(
            "TKT-20260802-0001", Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 2),
            name, "01700000000", 800m, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Sell_WithZeroFare_ThrowsDomainException()
    {
        var act = () => Ticket.Sell(
            "TKT-20260802-0001", Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 2),
            "Rahim Uddin", "01700000000", 0m, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_BeforeDeparture_SetsStatusToCancelled()
    {
        var ticket = SellTicket();
        var departure = DateTimeOffset.UtcNow.AddHours(2);

        ticket.Cancel("Passenger changed plans", Guid.NewGuid(), DateTimeOffset.UtcNow, departure);

        ticket.Status.Should().Be(TicketStatus.Cancelled);
        ticket.CancellationReason.Should().Be("Passenger changed plans");
    }

    [Fact]
    public void Cancel_AfterDeparture_ThrowsBusinessRuleViolationException()
    {
        var ticket = SellTicket();
        var departure = DateTimeOffset.UtcNow.AddHours(-1); // already departed

        var act = () => ticket.Cancel("Too late", Guid.NewGuid(), DateTimeOffset.UtcNow, departure);

        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*departure*");
    }

    [Fact]
    public void Cancel_ATicketAlreadyCancelled_ThrowsBusinessRuleViolationException()
    {
        var ticket = SellTicket();
        var departure = DateTimeOffset.UtcNow.AddHours(2);
        ticket.Cancel("First cancellation", Guid.NewGuid(), DateTimeOffset.UtcNow, departure);

        var act = () => ticket.Cancel("Second attempt", Guid.NewGuid(), DateTimeOffset.UtcNow, departure);

        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*already cancelled*");
    }

    [Fact]
    public void Cancel_WithoutReason_ThrowsDomainException()
    {
        var ticket = SellTicket();
        var departure = DateTimeOffset.UtcNow.AddHours(2);

        var act = () => ticket.Cancel("", Guid.NewGuid(), DateTimeOffset.UtcNow, departure);

        act.Should().Throw<DomainException>();
    }
}
