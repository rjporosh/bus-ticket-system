# Backend Guide

## Overview

The backend (`src/`) follows Clean Architecture with vertical-slice CQRS:

```
Domain            ← entities, enums, exceptions, interfaces
Application       ← commands, queries, validators, DTOs, behaviors
Infrastructure    ← EF Core, migrations, services, DI registration
Presentation      ← controllers, middleware, Program.cs
```

## How to Add a New CRUD Endpoint (Step by Step)

### Example: Adding "Banners" management

### 1. Create the entity (Domain)
`src/Core/BusTicketing.Domain/Entities/Banner.cs`:
```csharp
using BusTicketing.Domain.Common;

namespace BusTicketing.Domain.Entities;

public class Banner : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string ImageUrl { get; private set; } = default!;
    public string? TargetUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Banner() { } // EF Core

    public static Banner Create(string title, string imageUrl, string? targetUrl, int displayOrder)
    {
        return new Banner
        {
            Title = title,
            ImageUrl = imageUrl,
            TargetUrl = targetUrl,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }
}
```

### 2. Add DbSet and configuration (Infrastructure)
In `ApplicationDbContext.cs`:
```csharp
public DbSet<Banner> Banners => Set<Banner>();
```

Create `src/Infrastructure/BusTicketing.Infrastructure/Persistence/Configurations/BannerConfiguration.cs`:
```csharp
using BusTicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTicketing.Infrastructure.Persistence.Configurations;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
        builder.Property(b => b.ImageUrl).HasMaxLength(500).IsRequired();
        builder.HasIndex(b => b.DisplayOrder);
    }
}
```

Register it in `ApplicationDbContext.OnModelCreating` or let it auto-discover.

### 3. Create DTOs (Application)
`src/Core/BusTicketing.Application/Features/Banners/BannerDtos.cs`:
```csharp
namespace BusTicketing.Application.Features.Banners;

public record BannerDto(Guid Id, string Title, string ImageUrl, string? TargetUrl, int DisplayOrder, bool IsActive);
public record CreateBannerRequest(string Title, string ImageUrl, string? TargetUrl, int DisplayOrder);
public record UpdateBannerRequest(string Title, string ImageUrl, string? TargetUrl, int DisplayOrder, bool IsActive);
```

### 4. Create the Query (Application)
`src/Core/BusTicketing.Application/Features/Banners/GetBanners.cs`:
```csharp
using BusTicketing.Application.Common.Interfaces;
using MediatR;

namespace BusTicketing.Application.Features.Banners;

public record GetBannersQuery : IRequest<Result<List<BannerDto>>>;

public class GetBannersQueryHandler : IRequestHandler<GetBannersQuery, Result<List<BannerDto>>>
{
    private readonly IApplicationDbContext _db;
    public GetBannersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<BannerDto>>> Handle(GetBannersQuery request, CancellationToken cancellationToken)
    {
        var banners = await _db.Banners
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new BannerDto(b.Id, b.Title, b.ImageUrl, b.TargetUrl, b.DisplayOrder, b.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success(banners);
    }
}
```

### 5. Create the Command (Application)
`src/Core/BusTicketing.Application/Features/Banners/CreateBanner.cs`:
```csharp
using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BusTicketing.Application.Features.Banners;

public record CreateBannerCommand(string Title, string ImageUrl, string? TargetUrl, int DisplayOrder)
    : IRequest<Result<BannerDto>>;

public class CreateBannerCommandValidator : AbstractValidator<CreateBannerCommand>
{
    public CreateBannerCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateBannerCommandHandler : IRequestHandler<CreateBannerCommand, Result<BannerDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateBannerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<BannerDto>> Handle(CreateBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = Banner.Create(request.Title, request.ImageUrl, request.TargetUrl, request.DisplayOrder);
        _db.Banners.Add(banner);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new BannerDto(banner.Id, banner.Title, banner.ImageUrl, banner.TargetUrl, banner.DisplayOrder, banner.IsActive));
    }
}
```

### 6. Create the Controller (Presentation)
`src/Presentation/BusTicketing.Api/Controllers/V1/BannersController.cs`:
```csharp
using Asp.Versioning;
using BusTicketing.Application.Features.Banners;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/banners")]
[Authorize(Policy = "Permission:AdminAccess")]
public class BannersController : ControllerBase
{
    private readonly ISender _sender;
    public BannersController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<BannerDto>>> Get(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetBannersQuery(), cancellationToken));

    [HttpPost]
    public async Task<IResult> Create([FromBody] CreateBannerCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToApiResult(b => Created($"/api/v1/banners/{b.Id}", b));
    }
}
```

### 7. Add permission (Domain/Infrastructure)
Add to `Permission` enum in `src/Core/BusTicketing.Domain/Enums/Permission.cs`:
```csharp
AdminAccess = 99,
```

Seed it in `DataSeeder.cs` if needed.

### 8. Run migration
```bash
cd src/Presentation/BusTicketing.Api
dotnet ef migrations add AddBanners --project ../Infrastructure/BusTicketing.Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure/BusTicketing.Infrastructure --startup-project .
```

### 9. Test
```bash
dotnet test
```

## How to Add a Background Service

### Example: Ticket expiry cleanup job

Create `src/Infrastructure/BusTicketing.Infrastructure/Services/TicketCleanupBackgroundService.cs`:
```csharp
using BusTicketing.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusTicketing.Infrastructure.Services;

public class TicketCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketCleanupBackgroundService> _logger;

    public TicketCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<TicketCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ticket Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
                var oldTickets = await db.Tickets
                    .Where(t => t.TravelDate < cutoff && t.Status == 1 && t.IsDeleted == false)
                    .ToListAsync(stoppingToken);

                foreach (var ticket in oldTickets)
                {
                    ticket.SetDeleted(DateTimeOffset.UtcNow, "system-cleanup");
                }

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Cleaned up {Count} old cancelled tickets", oldTickets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ticket cleanup");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<TicketCleanupBackgroundService>();
```

## How to Add a Cron Job

For recurring tasks that need cron-like precision, use `Cronos` or `NCrontab`:

```bash
dotnet add package Cronos
```

Create `src/Infrastructure/BusTicketing.Infrastructure/Services/CronJobService.cs`:
```csharp
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusTicketing.Infrastructure.Services;

public class CronJobService : BackgroundService
{
    private readonly CronExpression _cron;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CronJobService> _logger;

    public CronJobService(IServiceProvider serviceProvider, ILogger<CronJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cron = CronExpression.Parse("0 0 2 * * ?"); // Daily at 2 AM
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var next = _cron.GetNextOccurrence(DateTimeOffset.UtcNow, Cronos.CronLocale.UTC);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = next - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay.Value, stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                // Resolve and execute your job here
                _logger.LogInformation("Cron job executed at {Time}", DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cron job failed");
            }

            next = _cron.GetNextOccurrence(DateTimeOffset.UtcNow, Cronos.CronLocale.UTC);
        }
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<CronJobService>();
```

## Key Conventions

| Aspect | Convention |
|--------|-----------|
| Feature folder | `Application/Features/{FeatureName}/` |
| Command/Query | `{Verb}{Feature}.cs` (e.g., `CreateBannerCommand`) |
| Handler | `{Verb}{Feature}Handler.cs` |
| Validator | `{Verb}{Feature}Validator.cs` |
| DTOs | `{Feature}Dtos.cs` |
| Controller | `{Feature}sController.cs` (plural) |
| Route | `/api/v1/{feature-name-plural}` |
| Permission | Add to `Permission` enum, use `[Authorize(Policy = "Permission:...")]` |
| Tests | Unit: `tests/BusTicketing.UnitTests/Application/`; Integration: `tests/BusTicketing.IntegrationTests/` |
