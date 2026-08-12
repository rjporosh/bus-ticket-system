using BusTicketing.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BusTicketing.Infrastructure.Services;

namespace BusTicketing.Api.Middleware;

public class GracefulShutdownMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICustomLogger _customLogger;
    private readonly ILogger<GracefulShutdownMiddleware> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public GracefulShutdownMiddleware(
        RequestDelegate next,
        ICustomLogger customLogger,
        ILogger<GracefulShutdownMiddleware> logger,
        IHostApplicationLifetime lifetime)
    {
        _next = next;
        _customLogger = customLogger;
        _logger = logger;
        _lifetime = lifetime;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("Service temporarily unavailable. Please try again later.");
            return;
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex) when (_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            // The app started shutting down while this request was in flight.
            // This is the only case this middleware should turn into its own
            // response; log it and report 503 rather than the generic error.
            _logger.LogWarning(ex, "Request aborted during application shutdown: {Method} {Path}", context.Request.Method, context.Request.Path);
            await _customLogger.LogRuntimeErrorAsync(
                $"Request aborted during shutdown: {context.Request.Method} {context.Request.Path}",
                ex);
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("Service temporarily unavailable. Please try again later.");
        }
        // Any other exception is NOT a shutdown scenario: let it propagate so
        // GlobalExceptionMiddleware (which wraps this middleware) can produce
        // the correct, localized, problem+json response with CORS headers
        // already applied -- rather than this middleware swallowing it behind
        // a bare 503 text response.
    }
}

public static class GracefulShutdownMiddlewareExtensions
{
    public static IApplicationBuilder UseGracefulShutdown(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GracefulShutdownMiddleware>();
    }
}