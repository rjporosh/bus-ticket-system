using System.Linq;
using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record GetAvailableSeatsQuery(Guid ScheduleId, DateOnly TravelDate) : IRequest<Result<List<SeatAvailabilityDto>>>;

public class GetAvailableSeatsQueryHandler : IRequestHandler<GetAvailableSeatsQuery, Result<List<SeatAvailabilityDto>>>
{
    private readonly IApplicationDbContext _db;
    public GetAvailableSeatsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<SeatAvailabilityDto>>> Handle(GetAvailableSeatsQuery request, CancellationToken cancellationToken)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);
        if (schedule is null)
            return Result.Failure<List<SeatAvailabilityDto>>(Error.NotFound("Schedule was not found."));

        var layout = await _db.SeatLayouts
            .Include(l => l.Seats)
            .FirstOrDefaultAsync(l => l.BusId == schedule.BusId, cancellationToken);
        if (layout is null)
            return Result.Failure<List<SeatAvailabilityDto>>(Error.NotFound("This bus has no seat layout configured."));

        var soldTickets = await _db.Tickets
            .Where(t => t.ScheduleId == request.ScheduleId && t.TravelDate == request.TravelDate && t.Status == TicketStatus.Sold)
            .Select(t => new { t.SeatId, t.PassengerName, t.Gender, t.Age })
            .ToListAsync(cancellationToken);

        var soldMap = soldTickets.ToDictionary(t => t.SeatId, t => (t.PassengerName, t.Gender, t.Age));

        var seats = layout.Seats
            .OrderBy(s => s.RowLabel).ThenBy(s => s.ColumnNumber)
            .ToList();

        var result = new List<SeatAvailabilityDto>();

        if (layout.LayoutType == LayoutType.RealBus && !string.IsNullOrWhiteSpace(layout.LayoutConfigJson))
        {
            MapRealBusSeats(seats, layout.LayoutConfigJson, layout.Columns, soldMap, result);
        }
        else
        {
            foreach (var seat in seats)
            {
                var isSold = soldMap.TryGetValue(seat.Id, out var soldInfo);
                var rowVisual = seat.RowLabel[0] - 'A' + 1;
                result.Add(new SeatAvailabilityDto(seat.Id, seat.SeatNumber, seat.RowLabel, seat.ColumnNumber, seat.Class, seat.IsActive, isSold, false, rowVisual, seat.ColumnNumber, soldInfo.PassengerName, soldInfo.Gender, soldInfo.Age));
            }
        }

        return Result.Success(result);
    }

    private static void MapRealBusSeats(IList<Seat> seats, string layoutConfigJson, int columns, Dictionary<Guid, (string PassengerName, string? Gender, int? Age)> soldMap, List<SeatAvailabilityDto> result)
    {
        var config = System.Text.Json.JsonSerializer.Deserialize<RealBusConfig>(layoutConfigJson) ?? new RealBusConfig();
        var rowSeats = config.SeatsPerRow ?? new List<RowSeatGroup>();
        var totalRows = seats.Max(s => s.RowLabel[0] - 'A') + 1;
        var defaultLeft = (columns + 1) / 2;
        var defaultRight = columns / 2;

        string? currentRowLabel = null;
        int seatIndexInRow = 0;
        int leftCount = defaultLeft;
        int rightCount = defaultRight;
        int aisleGap = config.AisleGap;
        bool driverInFirstRow = config.DriverSeat && seats.Any(s => s.RowLabel == "A" && s.IsDriver);

        foreach (var seat in seats)
        {
            if (seat.RowLabel != currentRowLabel)
            {
                currentRowLabel = seat.RowLabel;
                seatIndexInRow = 0;
                var rowIdx = seat.RowLabel[0] - 'A';
                var isOverriddenLastRow = rowIdx == totalRows - 1 && config.LastRowConfig != null;
                if (isOverriddenLastRow)
                {
                    leftCount = config.LastRowConfig!.Left;
                    rightCount = config.LastRowConfig!.Right;
                }
                else
                {
                    leftCount = rowSeats.Count > rowIdx ? rowSeats[rowIdx].Left : defaultLeft;
                    rightCount = rowSeats.Count > rowIdx ? rowSeats[rowIdx].Right : defaultRight;
                }
                // An overridden last row is a continuous run of seats with no
                // walking aisle: render it without the gap column.
                aisleGap = isOverriddenLastRow ? 0 : config.AisleGap;
            }

            if (seat.IsDriver)
            {
                var totalWidth = leftCount + aisleGap + rightCount;
                var centerCol = (totalWidth + 1) / 2;
                result.Add(new SeatAvailabilityDto(seat.Id, seat.SeatNumber, seat.RowLabel, seat.ColumnNumber, seat.Class, seat.IsActive, false, true, 1, centerCol));
            }
            else
            {
                int visualCol;
                if (seatIndexInRow < leftCount)
                {
                    visualCol = seatIndexInRow + 1;
                }
                else
                {
                    visualCol = leftCount + aisleGap + (seatIndexInRow - leftCount) + 1;
                }

                var rowVisual = seat.RowLabel == "A" && driverInFirstRow
                    ? 2
                    : (seat.RowLabel[0] - 'A' + 1 + (driverInFirstRow ? 1 : 0));

                var isSold = soldMap.TryGetValue(seat.Id, out var soldInfo);
                result.Add(new SeatAvailabilityDto(seat.Id, seat.SeatNumber, seat.RowLabel, seat.ColumnNumber, seat.Class, seat.IsActive, isSold, false, rowVisual, visualCol, soldInfo.PassengerName, soldInfo.Gender, soldInfo.Age));
                seatIndexInRow++;
            }
        }
    }
}
