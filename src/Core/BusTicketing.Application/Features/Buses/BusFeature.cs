using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using BusTicketing.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Buses;

public record BusDto(
    Guid Id,
    string Number,
    string RegistrationNumber,
    string OperatorName,
    int TotalSeats,
    bool IsActive,
    int SeatLayoutRows,
    int SeatLayoutColumns);

/// <summary>
/// Creates a bus and, in the same transaction, generates its seat layout
/// (Rows x Columns, e.g. 6x4 = 24 seats) so a bus is never left without a
/// bookable seat map.
/// </summary>
public record CreateBusCommand(
    string Number,
    string RegistrationNumber,
    string OperatorName,
    int Rows,
    int Columns,
    SeatClass DefaultSeatClass) : IRequest<Result<BusDto>>;

public class CreateBusCommandValidator : AbstractValidator<CreateBusCommand>
{
    public CreateBusCommandValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(30);
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Rows).InclusiveBetween(1, 26);
        RuleFor(x => x.Columns).InclusiveBetween(1, 10);
    }
}

public class CreateBusCommandHandler : IRequestHandler<CreateBusCommand, Result<BusDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateBusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<BusDto>> Handle(CreateBusCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _db.Buses.AnyAsync(b => b.RegistrationNumber == request.RegistrationNumber.ToUpper(), cancellationToken);
        if (duplicate)
            return Result.Failure<BusDto>(Error.Conflict($"A bus with registration \"{request.RegistrationNumber}\" already exists."));

        var totalSeats = request.Rows * request.Columns;
        var bus = Bus.Create(request.Number, request.RegistrationNumber, request.OperatorName, totalSeats);
        var layout = SeatLayout.Generate(bus.Id, request.Rows, request.Columns, request.DefaultSeatClass);
        bus.AssignSeatLayout(layout);

        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);

        _db.Buses.Add(bus);
        _db.SeatLayouts.Add(layout);
        foreach (var seat in layout.Seats)
            _db.Seats.Add(seat);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new BusDto(
            bus.Id, bus.Number, bus.RegistrationNumber, bus.OperatorName, bus.TotalSeats,
            bus.IsActive, layout.Rows, layout.Columns));
    }
}

public record UpdateBusCommand(Guid Id, string Number, string OperatorName) : IRequest<Result<BusDto>>;

public class UpdateBusCommandValidator : AbstractValidator<UpdateBusCommand>
{
    public UpdateBusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(30);
    }
}

public class UpdateBusCommandHandler : IRequestHandler<UpdateBusCommand, Result<BusDto>>
{
    private readonly IApplicationDbContext _db;
    public UpdateBusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<BusDto>> Handle(UpdateBusCommand request, CancellationToken cancellationToken)
    {
        var bus = await _db.Buses.Include(b => b.SeatLayout).FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
        if (bus is null)
            return Result.Failure<BusDto>(Error.NotFound($"Bus {request.Id} was not found."));

        bus.Update(request.Number, request.OperatorName);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<BusDto>(Error.Conflict("This bus was modified by someone else. Please reload and try again."));
        }

        return Result.Success(new BusDto(
            bus.Id, bus.Number, bus.RegistrationNumber, bus.OperatorName, bus.TotalSeats,
            bus.IsActive, bus.SeatLayout?.Rows ?? 0, bus.SeatLayout?.Columns ?? 0));
    }
}

public record SetBusActiveCommand(Guid Id, bool IsActive) : IRequest<Result>;

public class SetBusActiveCommandHandler : IRequestHandler<SetBusActiveCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public SetBusActiveCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(SetBusActiveCommand request, CancellationToken cancellationToken)
    {
        var bus = await _db.Buses.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
        if (bus is null)
            return Result.Failure(Error.NotFound($"Bus {request.Id} was not found."));

        if (request.IsActive) bus.Activate(); else bus.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public record GetBusesQuery(string? Search, bool? IsActive, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<BusDto>>;

public class GetBusesQueryHandler : IRequestHandler<GetBusesQuery, PaginatedList<BusDto>>
{
    private readonly IApplicationDbContext _db;
    public GetBusesQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<BusDto>> Handle(GetBusesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Buses.Include(b => b.SeatLayout).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(b => b.Number.ToLower().Contains(term) || b.RegistrationNumber.ToLower().Contains(term));
        }

        if (request.IsActive.HasValue)
            query = query.Where(b => b.IsActive == request.IsActive.Value);

        var projected = query.OrderBy(b => b.Number).Select(b => new BusDto(
            b.Id, b.Number, b.RegistrationNumber, b.OperatorName, b.TotalSeats, b.IsActive,
            b.SeatLayout != null ? b.SeatLayout.Rows : 0,
            b.SeatLayout != null ? b.SeatLayout.Columns : 0));

        return PaginatedList<BusDto>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public record GetBusByIdQuery(Guid Id) : IRequest<Result<BusDto>>;

public class GetBusByIdQueryHandler : IRequestHandler<GetBusByIdQuery, Result<BusDto>>
{
    private readonly IApplicationDbContext _db;
    public GetBusByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<BusDto>> Handle(GetBusByIdQuery request, CancellationToken cancellationToken)
    {
        var bus = await _db.Buses.Include(b => b.SeatLayout).FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
        return bus is null
            ? Result.Failure<BusDto>(Error.NotFound($"Bus {request.Id} was not found."))
            : Result.Success(new BusDto(
                bus.Id, bus.Number, bus.RegistrationNumber, bus.OperatorName, bus.TotalSeats,
                bus.IsActive, bus.SeatLayout?.Rows ?? 0, bus.SeatLayout?.Columns ?? 0));
    }
}
