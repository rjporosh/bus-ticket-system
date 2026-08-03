using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Features.Auth;

public record LogoutCommand(string RefreshToken) : IRequest<Result>;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public LogoutCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == request.RefreshToken), cancellationToken);

        if (user is null)
            return Result.Success(); // idempotent: already logged out / unknown token

        var token = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);
        token?.Revoke();

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
