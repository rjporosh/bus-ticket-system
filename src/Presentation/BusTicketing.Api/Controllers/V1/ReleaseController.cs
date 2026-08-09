using Asp.Versioning;
using BusTicketing.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketing.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/release")]
[Produces("application/json")]
public class ReleaseController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReleaseController> _logger;

    public ReleaseController(IWebHostEnvironment env, ILogger<ReleaseController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpGet("current")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReleaseInfoDto), StatusCodes.Status200OK)]
    public IActionResult GetCurrentRelease()
    {
        var notesPath = Path.Combine(_env.ContentRootPath, "release", "new-release.md");
        var markdown = System.IO.File.Exists(notesPath)
            ? System.IO.File.ReadAllText(notesPath)
            : "# Release Notes\n\nNo release notes available.";

        return Ok(new ReleaseInfoDto
        {
            Version = "1.0.0",
            ReleasedOn = new DateOnly(2026, 8, 9),
            Features = new List<string>
            {
                "Phase 1: Foundation + Fleet Operations (Auth, Users, Roles, Stations, Routes, Buses, Seat Layouts, Schedules)",
                "Phase 2: Booking, Mock Payment, Real Dashboard (Ticket sell/cancel/search, mock payment capture, double-booking prevention)",
                "Phase 3: Client-facing portal (Separate Angular client portal for public trip search and booking)",
                "Phase 4: Database artifacts (schema.sql, stored procedures, functions, views, triggers, seed data)",
                "Phase 4: Release endpoint for SQA team",
                "Phase 4: Configurable real-bus seat layout (backend + frontend grid)",
                "Phase 4: Postman collection with prescripts for multi-role token management"
            },
            BugsResolved = new List<string>
            {
                "Ticket number generation moved from COUNT-based query to DB sequence per date",
                "Per-provider migrations configured for PostgreSQL, SQL Server, MySQL, Oracle",
                "Rate limiting on /auth/login (5 attempts/minute)",
                "Fine-grained permission claims per role",
                "Standard security headers (HSTS, X-Content-Type-Options, etc.)",
                "Client booking component now renders real bus seat grid with driver seat and aisle gaps",
                "Seat layout configurable via LayoutType (StandardGrid vs RealBus)"
            },
            MarkdownNotes = markdown
        });
    }

    [HttpGet("notes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult GetReleaseNotesMarkdown()
    {
        var notesPath = Path.Combine(_env.ContentRootPath, "release", "new-release.md");
        if (!System.IO.File.Exists(notesPath))
            return NotFound("Release notes not found.");

        var markdown = System.IO.File.ReadAllText(notesPath);
        return Content(markdown, "text/markdown");
    }
}

public class ReleaseInfoDto
{
    public string Version { get; set; } = default!;
    public DateOnly ReleasedOn { get; set; }
    public List<string> Features { get; set; } = new();
    public List<string> BugsResolved { get; set; } = new();
    public string MarkdownNotes { get; set; } = default!;
}
