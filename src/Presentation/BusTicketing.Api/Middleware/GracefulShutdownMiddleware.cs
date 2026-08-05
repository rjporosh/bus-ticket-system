using BusTicketing.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BusTicketing.Infrastructure.Services;

namespace BusTicketing.Api.Middleware;

public class GracefulShutdownMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICustomLogger _customLogger;
    private readonly ILogger<GracefulShutdownMiddleware> _logger;

    public GracefulShutdownMiddleware(
        RequestDelegate next,
        ICustomLogger customLogger,
        ILogger<GracefulShutdownMiddleware> logger)
    {
        _next = next;
        _customLogger = customLogger;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical unhandled exception during request processing");
            await _customLogger.LogRuntimeErrorAsync(
                $"CRITICAL: {context.Request.Method} {context.Request.Path} - {ex.Message}",
                ex);

            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("Service temporarily unavailable. Please try again later.");
        }
    }
}

public static class GracefulShutdownMiddlewareExtensions
{
    public static IApplicationBuilder UseGracefulShutdown(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GracefulShutdownMiddleware>();
    }
}