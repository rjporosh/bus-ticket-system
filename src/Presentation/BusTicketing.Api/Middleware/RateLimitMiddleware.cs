using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace BusTicketing.Api.Middleware;

public class RateLimitMiddleware
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly ConcurrentDictionary<string, RequestRecord> _requests = new();

    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (string.Equals(context.Request.Path.Value, "/api/v1/auth/login", StringComparison.OrdinalIgnoreCase)
            && context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var key = GetClientKey(context);
            var record = _requests.GetOrAdd(key, _ => new RequestRecord());
            var now = DateTimeOffset.UtcNow;

            if (now - record.WindowStart > Window)
            {
                record.Reset(now);
            }

            record.Count++;

            if (record.Count > MaxFailedAttempts)
            {
                _logger.LogWarning("Rate limit exceeded for {Key}. {Count} requests in the last minute.", key, record.Count);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Too many requests.",
                    detail = "Please wait a minute before trying again.",
                    status = 429
                });
                return;
            }
        }

        await _next(context);
    }

    private static string GetClientKey(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwarded))
        {
            return forwarded.ToString().Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private sealed class RequestRecord
    {
        public int Count { get; set; }
        public DateTimeOffset WindowStart { get; set; } = DateTimeOffset.UtcNow;

        public void Reset(DateTimeOffset now)
        {
            Count = 0;
            WindowStart = now;
        }
    }
}
