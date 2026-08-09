using System.Linq;
using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.SeatLayouts;

public record SeatDto(Guid Id, string SeatNumber, string RowLabel, int ColumnNumber, SeatClass Class, bool IsActive, bool IsDriver = false, int? VisualRow = null, int? VisualCol = null);

public record SeatLayoutDto(Guid Id, Guid BusId, string BusNumber, int Rows, int Columns, LayoutType LayoutType, string? LayoutConfigJson, List<SeatDto> Seats);

public record GetSeatLayoutByBusIdQuery(Guid BusId) : IRequest<Result<SeatLayoutDto>>;

public class GetSeatLayoutByBusIdQueryHandler : IRequestHandler<GetSeatLayoutByBusIdQuery, Result<SeatLayoutDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSeatLayoutByBusIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<SeatLayoutDto>> Handle(GetSeatLayoutByBusIdQuery request, CancellationToken cancellationToken)
    {
        var layout = await _db.SeatLayouts
            .Include(l => l.Seats)
            .FirstOrDefaultAsync(l => l.BusId == request.BusId, cancellationToken);

        if (layout is null)
            return Result.Failure<SeatLayoutDto>(Error.NotFound($"No seat layout found for bus {request.BusId}."));

        var bus = await _db.Buses.FirstAsync(b => b.Id == request.BusId, cancellationToken);

        var seats = layout.Seats
            .OrderBy(s => s.RowLabel).ThenBy(s => s.ColumnNumber)
            .ToList();

        var seatDtos = new List<SeatDto>();

        if (layout.LayoutType == LayoutType.RealBus && !string.IsNullOrWhiteSpace(layout.LayoutConfigJson))
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<RealBusConfig>(layout.LayoutConfigJson) ?? new RealBusConfig();
            var rowSeats = config.SeatsPerRow ?? new List<RowSeatGroup>();
            var totalRows = seats.Max(s => s.RowLabel[0] - 'A') + 1;

            string? currentRowLabel = null;
            int seatIndexInRow = 0;
            int leftCount = 2;
            int rightCount = 2;
            bool driverInFirstRow = config.DriverSeat && seats.Any(s => s.RowLabel == "A" && s.IsDriver);

            foreach (var seat in seats)
            {
                if (seat.RowLabel != currentRowLabel)
                {
                    currentRowLabel = seat.RowLabel;
                    seatIndexInRow = 0;
                    var rowIdx = seat.RowLabel[0] - 'A';
                    if (rowIdx == totalRows - 1 && config.LastRowConfig != null)
                    {
                        leftCount = config.LastRowConfig.Left;
                        rightCount = config.LastRowConfig.Right;
                    }
                    else
                    {
                        leftCount = rowSeats.Count > rowIdx ? rowSeats[rowIdx].Left : 2;
                        rightCount = rowSeats.Count > rowIdx ? rowSeats[rowIdx].Right : 2;
                    }
                }

                if (seat.IsDriver)
                {
                    var totalWidth = leftCount + config.AisleGap + rightCount;
                    var centerCol = (totalWidth + 1) / 2;
                    seatDtos.Add(new SeatDto(seat.Id, seat.SeatNumber, seat.RowLabel, seat.ColumnNumber, seat.Class, seat.IsActive, seat.IsDriver, 1, centerCol));
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

                    seatDtos.Add(new SeatDto(seat.Id, seat.SeatNumber, seat.RowLabel, seat.ColumnNumber, seat.Class, seat.IsActive, seat.IsDriver, rowVisual, visualCol));
                    seatIndexInRow++;
                }
            }
        }
        else
        {
            seatDtos.AddRange(seats.Select(s => new SeatDto(s.Id, s.SeatNumber, s.RowLabel, s.ColumnNumber, s.Class, s.IsActive, s.IsDriver)));
        }

        return Result.Success(new SeatLayoutDto(layout.Id, bus.Id, bus.Number, layout.Rows, layout.Columns, layout.LayoutType, layout.LayoutConfigJson, seatDtos));
    }
}

public record SetSeatServiceStatusCommand(Guid SeatId, bool IsActive) : IRequest<Result>;

public class SetSeatServiceStatusCommandValidator : AbstractValidator<SetSeatServiceStatusCommand>
{
    public SetSeatServiceStatusCommandValidator()
    {
        RuleFor(x => x.SeatId).NotEmpty();
    }
}

public class SetSeatServiceStatusCommandHandler : IRequestHandler<SetSeatServiceStatusCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public SetSeatServiceStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(SetSeatServiceStatusCommand request, CancellationToken cancellationToken)
    {
        var seat = await _db.Seats.FirstOrDefaultAsync(s => s.Id == request.SeatId, cancellationToken);
        if (seat is null)
            return Result.Failure(Error.NotFound($"Seat {request.SeatId} was not found."));

        if (request.IsActive) seat.SetInService(); else seat.SetOutOfService();
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public record ReclassifySeatCommand(Guid SeatId, SeatClass Class) : IRequest<Result>;

public class ReclassifySeatCommandHandler : IRequestHandler<ReclassifySeatCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public ReclassifySeatCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(ReclassifySeatCommand request, CancellationToken cancellationToken)
    {
        var seat = await _db.Seats.FirstOrDefaultAsync(s => s.Id == request.SeatId, cancellationToken);
        if (seat is null)
            return Result.Failure(Error.NotFound($"Seat {request.SeatId} was not found."));

        seat.Reclassify(request.Class);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
