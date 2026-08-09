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
            .Select(t => new { t.SeatId, t.PassengerName, t.Gender })
            .ToListAsync(cancellationToken);

        var soldMap = soldTickets.ToDictionary(t => t.SeatId, t => (t.PassengerName, t.Gender));

        var seats = layout.Seats
            .OrderBy(s => s.RowLabel).ThenBy(s => s.ColumnNumber)
            .ToList();

        var result = new List<SeatAvailabilityDto>();

        if (layout.LayoutType == LayoutType.RealBus && !string.IsNullOrWhiteSpace(layout.LayoutConfigJson))
        {
            MapRealBusSeats(seats, layout.LayoutConfigJson, soldMap, result);
        }
        else
        {
            foreach (var seat in seats)
            {
                var isSold = soldMap.TryGetValue(seat.Id, out var soldInfo);
                var rowVisual = seat.RowLabel[0] - 'A' + 1;
                result.Add(new SeatAvailabilityDto(seat.Id, seat.SeatNumber, seat.RowLabel, seat.ColumnNumber, seat.Class, seat.IsActive, isSold, false, rowVisual, seat.ColumnNumber, soldInfo.PassengerName, soldInfo.Gender));
            }
        }

        return Result.Success(result);
    }

    private static void MapRealBusSeats(IList<Seat> seats, string layoutConfigJson, Dictionary<Guid, (string PassengerName, string? Gender)> soldMap, List<SeatAvailabilityDto> result)
    {
        var config = System.Text.Json.JsonSerializer.Deserialize<RealBusConfig>(layoutConfigJson) ?? new RealBusConfig();
        var rowSeats = config.SeatsPerRow ?? new List<RowSeatGroup>();

        string? currentRowLabel = null;
        int seatIndexInRow = 0;
        int leftCount = 2;
        int rightCount = 2;
        int expectedInRow = 0;
        bool driverInFirstRow = config.DriverSeat && seats.Any(s => s.RowLabel == "A" && s.IsDriver);

        foreach (var seat in seats)
        {
            if (seat.RowLabel != currentRowLabel)
            {
                currentRowLabel = seat.RowLabel;
                seatIndexInRow = 0;
                var rowIdx = seat.RowLabel[0] - 'A';
                leftCount = rowSeats.Count > rowIdx ? rowSeats[rowIdx].Left : 2;
                rightCount = rowSeats.Count > rowIdx ? rowSeats[rowIdx].Right : 2;
                expectedInRow = leftCount + rightCount + (rowIdx == 0 && config.DriverSeat ? 1 : 0);
            }

            if (seat.IsDriver)
            {
                var totalWidth = leftCount + config.AisleGap + rightCount;
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
                    visualCol = leftCount + config.AisleGap + (seatIndexInRow - leftCount) + 1;
                }

                var rowVisual = seat.RowLabel == "A" && driverInFirstRow
                    ? 2
                    : (seat.RowLabel[0] - 'A' + 1 + (driverInFirstRow ? 1 : 0));

                var isSold = soldMap.TryGetValue(seat.Id, out var soldInfo);
                result.Add(new SeatAvailabilityDto(seat.Id, seat.SeatNumber, seat.RowLabel, seat.ColumnNumber, seat.Class, seat.IsActive, isSold, false, rowVisual, visualCol, soldInfo.PassengerName, soldInfo.Gender));
                seatIndexInRow++;
            }
        }
    }
}
