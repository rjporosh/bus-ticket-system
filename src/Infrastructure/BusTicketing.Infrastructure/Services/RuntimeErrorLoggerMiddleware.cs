using System.Net;
using BusTicketing.Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BusTicketing.Infrastructure.Services;

public class RuntimeErrorLoggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICustomLogger _customLogger;
    private readonly ILogger<RuntimeErrorLoggerMiddleware> _logger;

    public RuntimeErrorLoggerMiddleware(
        RequestDelegate next,
        ICustomLogger customLogger,
        ILogger<RuntimeErrorLoggerMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception occurred");
            await _customLogger.LogRuntimeErrorAsync(
                $"Unhandled exception in {context.Request.Method} {context.Request.Path}",
                ex);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync("An unexpected error occurred. Please try again later.");
        }
    }
}

public static class RuntimeErrorLoggerMiddlewareExtensions
{
    public static IApplicationBuilder UseRuntimeErrorLogger(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RuntimeErrorLoggerMiddleware>();
    }
}