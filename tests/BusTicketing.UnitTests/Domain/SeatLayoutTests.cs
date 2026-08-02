using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BusTicketing.UnitTests.Domain;

public class SeatLayoutTests
{
    [Fact]
    public void Generate_SixByFour_Creates24SeatsLabelledCorrectly()
    {
        var layout = SeatLayout.Generate(Guid.NewGuid(), rows: 6, columns: 4);

        layout.Seats.Should().HaveCount(24);
        layout.Seats.Select(s => s.SeatNumber).Should().Contain(new[] { "A1", "A4", "F1", "F4" });
    }

    [Fact]
    public void Generate_RowLabelling_UsesSequentialLetters()
    {
        var layout = SeatLayout.Generate(Guid.NewGuid(), rows: 3, columns: 2);

        layout.Seats.Select(s => s.RowLabel).Distinct().Should().BeEquivalentTo(new[] { "A", "B", "C" });
    }

    [Fact]
    public void Generate_ZeroRows_ThrowsDomainException()
    {
        var act = () => SeatLayout.Generate(Guid.NewGuid(), rows: 0, columns: 4);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Generate_TooManyRows_ThrowsDomainException()
    {
        var act = () => SeatLayout.Generate(Guid.NewGuid(), rows: 27, columns: 4);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Bus_AssignSeatLayout_WithMismatchedSeatCount_ThrowsDomainException()
    {
        var bus = Bus.Create("Bus-1", "DHK-1", "Green Line", totalSeats: 24);
        var mismatchedLayout = SeatLayout.Generate(bus.Id, rows: 5, columns: 4); // 20 seats, not 24

        var act = () => bus.AssignSeatLayout(mismatchedLayout);
        act.Should().Throw<DomainException>().WithMessage("*24*");
    }

    [Fact]
    public void Seat_SetOutOfService_ThenSetInService_RoundTrips()
    {
        var layout = SeatLayout.Generate(Guid.NewGuid(), 1, 1);
        var seat = layout.Seats.Single();

        seat.SetOutOfService();
        seat.IsActive.Should().BeFalse();

        seat.SetInService();
        seat.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Seat_Reclassify_ChangesSeatClass()
    {
        var layout = SeatLayout.Generate(Guid.NewGuid(), 1, 1, SeatClass.Economy);
        var seat = layout.Seats.Single();

        seat.Reclassify(SeatClass.Business);
        seat.Class.Should().Be(SeatClass.Business);
    }
}
