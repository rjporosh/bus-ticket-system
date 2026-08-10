using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Features.Booking;
using BusTicketing.Domain.Enums;
using BusTicketing.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BusTicketing.UnitTests.Application;

public class PrintTicketServiceTests
{
    private readonly IPrintTicketService _service = new BusTicketing.Infrastructure.Services.PrintTicketService();

    [Fact]
    public async Task GenerateHtmlAsync_ReturnsNonEmptyHtml_WithTicketDetails()
    {
        var ticket = new PrintTicketDto(
            Guid.NewGuid(),
            "TKT-20260810-0001",
            "Bus-001",
            "Dhaka to Chittagong",
            "A1",
            new DateOnly(2026, 8, 15),
            new TimeOnly(8, 30),
            "John Doe",
            "01700000000",
            "1234567890",
            "Male",
            30,
            500m,
            TicketStatus.Sold,
            "admin",
            DateTimeOffset.UtcNow,
            null,
            null,
            string.Empty);

        var html = await _service.GenerateHtmlAsync(ticket);

        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("TKT-20260810-0001");
        html.Should().Contain("John Doe");
        html.Should().Contain("01700000000");
        html.Should().Contain("Dhaka to Chittagong");
        html.Should().Contain("Bus-001");
        html.Should().Contain("A1");
        html.Should().Contain("৳500.00");
        html.Should().Contain("Sold");
        html.Should().Contain("print()");
    }

    [Fact]
    public async Task GenerateHtmlAsync_IncludesCancellationDetails_WhenStatusIsCancelled()
    {
        var ticket = new PrintTicketDto(
            Guid.NewGuid(),
            "TKT-20260810-0002",
            "Bus-002",
            "Dhaka to Sylhet",
            "B2",
            new DateOnly(2026, 8, 16),
            new TimeOnly(10, 0),
            "Jane Smith",
            "01711111111",
            null,
            "Female",
            25,
            600m,
            TicketStatus.Cancelled,
            "booth",
            DateTimeOffset.UtcNow,
            "Passenger changed plan",
            DateTimeOffset.UtcNow.AddDays(-1),
            string.Empty);

        var html = await _service.GenerateHtmlAsync(ticket);

        html.Should().Contain("Cancelled");
        html.Should().Contain("Passenger changed plan");
        html.Should().Contain("cancellation-box");
    }

    [Fact]
    public async Task GenerateHtmlAsync_HandlesNullOptionalFields()
    {
        var ticket = new PrintTicketDto(
            Guid.NewGuid(),
            "TKT-20260810-0003",
            "Bus-003",
            "Dhaka to Rajshahi",
            "C3",
            new DateOnly(2026, 8, 17),
            new TimeOnly(14, 0),
            "Alice",
            "01722222222",
            null,
            null,
            null,
            450m,
            TicketStatus.Sold,
            "admin",
            DateTimeOffset.UtcNow,
            null,
            null,
            string.Empty);

        var html = await _service.GenerateHtmlAsync(ticket);

        html.Should().Contain("Alice");
        html.Should().Contain("—");
        html.Should().NotContain("<div class=\"cancellation-box\">");
    }
}
