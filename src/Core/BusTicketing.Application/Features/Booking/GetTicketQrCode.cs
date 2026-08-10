using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Booking;

public record GetTicketQrCodeQuery(Guid TicketId) : IRequest<Result<TicketQrCodeDto>>;

public record TicketQrCodeDto(Guid TicketId, string TicketNumber, string QrCodeBase64, string VerificationPayload);

public class GetTicketQrCodeQueryHandler : IRequestHandler<GetTicketQrCodeQuery, Result<TicketQrCodeDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IQrCodeService _qrCodeService;

    public GetTicketQrCodeQueryHandler(IApplicationDbContext db, IQrCodeService qrCodeService)
    {
        _db = db;
        _qrCodeService = qrCodeService;
    }

    public async Task<Result<TicketQrCodeDto>> Handle(GetTicketQrCodeQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Schedule).ThenInclude(s => s.Bus)
            .Include(t => t.Schedule).ThenInclude(s => s.Route)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket is null)
            return Result.Failure<TicketQrCodeDto>(Error.NotFound("Ticket not found."));

        var payload = $"TICKET:{ticket.TicketNumber}:{ticket.Id}:{ticket.Schedule.Bus.Number}:{ticket.Seat.SeatNumber}:{ticket.TravelDate:yyyyMMdd}";
        var qrBytes = await _qrCodeService.GeneratePngAsync(payload, cancellationToken);
        var qrBase64 = Convert.ToBase64String(qrBytes);

        return Result.Success(new TicketQrCodeDto(ticket.Id, ticket.TicketNumber, qrBase64, payload));
    }
}
