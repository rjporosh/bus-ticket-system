using System.Net;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Api.Middleware;

/// <summary>
/// Single choke point for turning exceptions into RFC 7807 ProblemDetails responses.
/// Handlers should prefer returning a failed Result for anticipated failures; this
/// middleware exists for the exceptions that still legitimately propagate
/// (FluentValidation's own exception type, EF concurrency conflicts, domain
/// exceptions raised deep in aggregate methods, and truly unexpected errors).
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "One or more validation errors occurred."),
            EntityNotFoundException => (HttpStatusCode.NotFound, "The requested resource was not found."),
            BusinessRuleViolationException => (HttpStatusCode.Conflict, "The request conflicts with the current state of the resource."),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "The resource was modified by another request. Please reload and try again."),
            DomainException => (HttpStatusCode.UnprocessableEntity, "The request violates a business rule."),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "You do not have permission to perform this action."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "Handled exception ({StatusCode}) processing {Method} {Path}", (int)statusCode, context.Request.Method, context.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.io/{(int)statusCode}"
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        if (_environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
            problemDetails.Extensions["exception"] = exception.ToString();

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}

/// <summary>Maps the Result/Result&lt;T&gt; pattern onto ASP.NET Core's IResult, so controllers stay a thin translation layer.</summary>
public static class ResultExtensions
{
    public static IResult ToApiResult(this Result result)
    {
        if (result.IsSuccess) return Microsoft.AspNetCore.Http.Results.NoContent();
        return Problem(result.Error);
    }

    public static IResult ToApiResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess is not null ? onSuccess(result.Value) : Microsoft.AspNetCore.Http.Results.Ok(result.Value);

        return Problem(result.Error);
    }

    private static IResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        if (error.Type == ErrorType.Validation && error.ValidationErrors is not null)
        {
            return Microsoft.AspNetCore.Http.Results.ValidationProblem(
                error.ValidationErrors.ToDictionary(kv => kv.Key, kv => kv.Value),
                title: error.Message);
        }

        return Microsoft.AspNetCore.Http.Results.Problem(
            title: error.Message,
            statusCode: statusCode,
            type: $"https://httpstatuses.io/{statusCode}");
    }
}
