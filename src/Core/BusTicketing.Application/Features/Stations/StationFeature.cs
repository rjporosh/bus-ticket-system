using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Stations;

public record StationDto(Guid Id, string Name, string City, string? Address, bool IsActive);

public record CreateStationCommand(string Name, string City, string? Address) : IRequest<Result<StationDto>>;

public class CreateStationCommandValidator : AbstractValidator<CreateStationCommand>
{
    public CreateStationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
    }
}

public class CreateStationCommandHandler : IRequestHandler<CreateStationCommand, Result<StationDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateStationCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<StationDto>> Handle(CreateStationCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _db.Stations.AnyAsync(s => s.Name == request.Name && s.City == request.City, cancellationToken);
        if (duplicate)
            return Result.Failure<StationDto>(Error.Conflict($"A station named \"{request.Name}\" already exists in {request.City}."));

        var station = Station.Create(request.Name, request.City, request.Address);
        _db.Stations.Add(station);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new StationDto(station.Id, station.Name, station.City, station.Address, station.IsActive));
    }
}

public record UpdateStationCommand(Guid Id, string Name, string City, string? Address) : IRequest<Result<StationDto>>;

public class UpdateStationCommandValidator : AbstractValidator<UpdateStationCommand>
{
    public UpdateStationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
    }
}

public class UpdateStationCommandHandler : IRequestHandler<UpdateStationCommand, Result<StationDto>>
{
    private readonly IApplicationDbContext _db;
    public UpdateStationCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<StationDto>> Handle(UpdateStationCommand request, CancellationToken cancellationToken)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (station is null)
            return Result.Failure<StationDto>(Error.NotFound($"Station {request.Id} was not found."));

        station.Update(request.Name, request.City, request.Address);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<StationDto>(Error.Conflict("This station was modified by someone else. Please reload and try again."));
        }

        return Result.Success(new StationDto(station.Id, station.Name, station.City, station.Address, station.IsActive));
    }
}

public record SetStationActiveCommand(Guid Id, bool IsActive) : IRequest<Result>;

public class SetStationActiveCommandHandler : IRequestHandler<SetStationActiveCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public SetStationActiveCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(SetStationActiveCommand request, CancellationToken cancellationToken)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (station is null)
            return Result.Failure(Error.NotFound($"Station {request.Id} was not found."));

        if (request.IsActive) station.Activate(); else station.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public record GetStationsQuery(string? Search, bool? IsActive, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<StationDto>>;

public class GetStationsQueryHandler : IRequestHandler<GetStationsQuery, PaginatedList<StationDto>>
{
    private readonly IApplicationDbContext _db;
    public GetStationsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<StationDto>> Handle(GetStationsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Stations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.City.ToLower().Contains(term));
        }

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        var projected = query.OrderBy(s => s.City).ThenBy(s => s.Name)
            .Select(s => new StationDto(s.Id, s.Name, s.City, s.Address, s.IsActive));

        return PaginatedList<StationDto>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public record GetStationByIdQuery(Guid Id) : IRequest<Result<StationDto>>;

public class GetStationByIdQueryHandler : IRequestHandler<GetStationByIdQuery, Result<StationDto>>
{
    private readonly IApplicationDbContext _db;
    public GetStationByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<StationDto>> Handle(GetStationByIdQuery request, CancellationToken cancellationToken)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        return station is null
            ? Result.Failure<StationDto>(Error.NotFound($"Station {request.Id} was not found."))
            : Result.Success(new StationDto(station.Id, station.Name, station.City, station.Address, station.IsActive));
    }
}
