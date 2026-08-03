using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Routes;

public record RouteDto(
    Guid Id,
    string Name,
    Guid OriginStationId,
    string OriginStationName,
    Guid DestinationStationId,
    string DestinationStationName,
    decimal DistanceKm,
    int EstimatedDurationMinutes,
    bool IsActive);

public record CreateRouteCommand(
    string Name,
    Guid OriginStationId,
    Guid DestinationStationId,
    decimal DistanceKm,
    int EstimatedDurationMinutes) : IRequest<Result<RouteDto>>;

public class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.OriginStationId).NotEmpty();
        RuleFor(x => x.DestinationStationId).NotEmpty()
            .NotEqual(x => x.OriginStationId).WithMessage("Origin and destination stations must be different.");
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0);
    }
}

public class CreateRouteCommandHandler : IRequestHandler<CreateRouteCommand, Result<RouteDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateRouteCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RouteDto>> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        var origin = await _db.Stations.FirstOrDefaultAsync(s => s.Id == request.OriginStationId, cancellationToken);
        if (origin is null)
            return Result.Failure<RouteDto>(Error.NotFound("Origin station was not found."));

        var destination = await _db.Stations.FirstOrDefaultAsync(s => s.Id == request.DestinationStationId, cancellationToken);
        if (destination is null)
            return Result.Failure<RouteDto>(Error.NotFound("Destination station was not found."));

        var duplicate = await _db.Routes.AnyAsync(r =>
            r.OriginStationId == request.OriginStationId && r.DestinationStationId == request.DestinationStationId,
            cancellationToken);
        if (duplicate)
            return Result.Failure<RouteDto>(Error.Conflict("A route between these two stations already exists."));

        var route = Domain.Entities.Route.Create(
            request.Name, request.OriginStationId, request.DestinationStationId,
            request.DistanceKm, request.EstimatedDurationMinutes);

        _db.Routes.Add(route);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new RouteDto(
            route.Id, route.Name, origin.Id, origin.Name, destination.Id, destination.Name,
            route.DistanceKm, route.EstimatedDurationMinutes, route.IsActive));
    }
}

public record UpdateRouteCommand(Guid Id, string Name, decimal DistanceKm, int EstimatedDurationMinutes)
    : IRequest<Result<RouteDto>>;

public class UpdateRouteCommandValidator : AbstractValidator<UpdateRouteCommand>
{
    public UpdateRouteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0);
    }
}

public class UpdateRouteCommandHandler : IRequestHandler<UpdateRouteCommand, Result<RouteDto>>
{
    private readonly IApplicationDbContext _db;
    public UpdateRouteCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RouteDto>> Handle(UpdateRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _db.Routes.Include(r => r.Origin).Include(r => r.Destination)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (route is null)
            return Result.Failure<RouteDto>(Error.NotFound($"Route {request.Id} was not found."));

        route.Update(request.Name, request.DistanceKm, request.EstimatedDurationMinutes);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<RouteDto>(Error.Conflict("This route was modified by someone else. Please reload and try again."));
        }

        return Result.Success(new RouteDto(
            route.Id, route.Name, route.OriginStationId, route.Origin.Name,
            route.DestinationStationId, route.Destination.Name,
            route.DistanceKm, route.EstimatedDurationMinutes, route.IsActive));
    }
}

public record SetRouteActiveCommand(Guid Id, bool IsActive) : IRequest<Result>;

public class SetRouteActiveCommandHandler : IRequestHandler<SetRouteActiveCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public SetRouteActiveCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(SetRouteActiveCommand request, CancellationToken cancellationToken)
    {
        var route = await _db.Routes.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (route is null)
            return Result.Failure(Error.NotFound($"Route {request.Id} was not found."));

        if (request.IsActive) route.Activate(); else route.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public record GetRoutesQuery(string? Search, Guid? OriginStationId, Guid? DestinationStationId, bool? IsActive, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<RouteDto>>;

public class GetRoutesQueryHandler : IRequestHandler<GetRoutesQuery, PaginatedList<RouteDto>>
{
    private readonly IApplicationDbContext _db;
    public GetRoutesQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<RouteDto>> Handle(GetRoutesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Routes.Include(r => r.Origin).Include(r => r.Destination).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(term));
        }

        if (request.OriginStationId.HasValue)
            query = query.Where(r => r.OriginStationId == request.OriginStationId.Value);

        if (request.DestinationStationId.HasValue)
            query = query.Where(r => r.DestinationStationId == request.DestinationStationId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(r => r.IsActive == request.IsActive.Value);

        var projected = query.OrderBy(r => r.Name).Select(r => new RouteDto(
            r.Id, r.Name, r.OriginStationId, r.Origin.Name, r.DestinationStationId, r.Destination.Name,
            r.DistanceKm, r.EstimatedDurationMinutes, r.IsActive));

        return PaginatedList<RouteDto>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public record GetRouteByIdQuery(Guid Id) : IRequest<Result<RouteDto>>;

public class GetRouteByIdQueryHandler : IRequestHandler<GetRouteByIdQuery, Result<RouteDto>>
{
    private readonly IApplicationDbContext _db;
    public GetRouteByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RouteDto>> Handle(GetRouteByIdQuery request, CancellationToken cancellationToken)
    {
        var route = await _db.Routes.Include(r => r.Origin).Include(r => r.Destination)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        return route is null
            ? Result.Failure<RouteDto>(Error.NotFound($"Route {request.Id} was not found."))
            : Result.Success(new RouteDto(
                route.Id, route.Name, route.OriginStationId, route.Origin.Name,
                route.DestinationStationId, route.Destination.Name,
                route.DistanceKm, route.EstimatedDurationMinutes, route.IsActive));
    }
}
