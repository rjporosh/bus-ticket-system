using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Roles;

public record RoleDto(Guid Id, string Name, string? Description, bool IsSystemRole);

public record CreateRoleCommand(string Name, string? Description) : IRequest<Result<RoleDto>>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    private readonly IApplicationDbContext _db;

    public CreateRoleCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken);
        if (exists)
            return Result.Failure<RoleDto>(Error.Conflict($"Role \"{request.Name}\" already exists."));

        var role = Role.Create(request.Name, request.Description);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new RoleDto(role.Id, role.Name, role.Description, role.IsSystemRole));
    }
}

public record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest<Result<RoleDto>>;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<RoleDto>>
{
    private readonly IApplicationDbContext _db;

    public UpdateRoleCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (role is null)
            return Result.Failure<RoleDto>(Error.NotFound($"Role {request.Id} was not found."));

        try
        {
            role.Update(request.Name, request.Description);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return Result.Failure<RoleDto>(Error.Conflict(ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(new RoleDto(role.Id, role.Name, role.Description, role.IsSystemRole));
    }
}

public record GetRolesQuery : IRequest<List<RoleDto>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly IApplicationDbContext _db;

    public GetRolesQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken) =>
        _db.Roles.OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystemRole))
            .ToListAsync(cancellationToken);
}
