using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record GetTicketPrintQuery(Guid TicketId) : IRequest<Result<PrintTicketDto>>;

public class GetTicketPrintQueryHandler : IRequestHandler<GetTicketPrintQuery, Result<PrintTicketDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPrintTicketService _printTicketService;

    public GetTicketPrintQueryHandler(IApplicationDbContext db, IPrintTicketService printTicketService)
    {
        _db = db;
        _printTicketService = printTicketService;
    }

    public async Task<Result<PrintTicketDto>> Handle(GetTicketPrintQuery request, CancellationToken cancellationToken)
    {
        var query = from t in _db.Tickets
            .Include(t => t.Schedule).ThenInclude(s => s.Bus)
            .Include(t => t.Schedule).ThenInclude(s => s.Route)
            .Include(t => t.Seat)
            join u in _db.Users on t.SoldByUserId equals u.Id into sellers
            from seller in sellers.DefaultIfEmpty()
            where t.Id == request.TicketId
            select new { Ticket = t, SellerUsername = seller != null ? seller.Username : "unknown" };

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            return Result.Failure<PrintTicketDto>(Error.NotFound("Ticket not found."));

        var dto = new PrintTicketDto(
            result.Ticket.Id,
            result.Ticket.TicketNumber,
            result.Ticket.Schedule.Bus.Number,
            result.Ticket.Schedule.Route.Name,
            result.Ticket.Seat.SeatNumber,
            result.Ticket.TravelDate,
            result.Ticket.Schedule.DepartureTime,
            result.Ticket.PassengerName,
            result.Ticket.MobileNumber,
            result.Ticket.NidOrPassport,
            result.Ticket.Gender,
            result.Ticket.Age,
            result.Ticket.FareAmount,
            result.Ticket.Status,
            result.SellerUsername,
            result.Ticket.SoldAtUtc,
            result.Ticket.CancellationReason,
            result.Ticket.CancelledAtUtc,
            string.Empty);

        dto = dto with { PrintableHtml = await _printTicketService.GenerateHtmlAsync(dto, cancellationToken) };

        return Result.Success(dto);
    }
}
