using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Users;

public record GetUsersQuery(
    string? Search,
    Guid? RoleId,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Users.Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.FullName.ToLower().Contains(term));
        }

        if (request.RoleId.HasValue)
            query = query.Where(u => u.RoleId == request.RoleId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        var projected = query
            .OrderBy(u => u.Username)
            .Select(u => new UserDto(
                u.Id, u.Username, u.Email, u.FullName, u.PhoneNumber,
                u.BoothName, u.IsActive, u.RoleId, u.Role.Name, u.CreatedAtUtc));

        return PaginatedList<UserDto>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUserByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.Include(u => u.Role)
            .Where(u => u.Id == request.Id)
            .Select(u => new UserDto(
                u.Id, u.Username, u.Email, u.FullName, u.PhoneNumber,
                u.BoothName, u.IsActive, u.RoleId, u.Role.Name, u.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? Result.Failure<UserDto>(Error.NotFound($"User {request.Id} was not found."))
            : Result.Success(user);
    }
}
