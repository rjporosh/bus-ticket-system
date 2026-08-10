using System.Net;
using BusTicketing.Application.Common.Localization;
using BusTicketing.Application.Common.Models;
using BusTicketing.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace BusTicketing.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment, IStringLocalizer<SharedResources> localizer)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _localizer = localizer;
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
        var (statusCode, titleKey) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "BadRequest"),
            EntityNotFoundException => (HttpStatusCode.NotFound, "NotFound"),
            BusinessRuleViolationException => (HttpStatusCode.Conflict, "Conflict"),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "ConcurrencyConflict"),
            DomainException => (HttpStatusCode.UnprocessableEntity, "BadRequest"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Forbidden"),
            _ => (HttpStatusCode.InternalServerError, "InternalServerError")
        };

        var title = _localizer[titleKey];

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

public static class ResultExtensions
{
    public static IResult ToApiResult(this Result result, IStringLocalizer? localizer = null)
    {
        if (result.IsSuccess) return Microsoft.AspNetCore.Http.Results.NoContent();
        return Problem(result.Error, localizer);
    }

    public static IResult ToApiResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null, IStringLocalizer? localizer = null)
    {
        if (result.IsSuccess)
            return onSuccess is not null ? onSuccess(result.Value) : Microsoft.AspNetCore.Http.Results.Ok(result.Value);

        return Problem(result.Error, localizer);
    }

    private static IResult Problem(Error error, IStringLocalizer? localizer = null)
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

        var title = localizer is not null ? localizer[error.Code] : error.Code;

        if (error.Type == ErrorType.Validation && error.ValidationErrors is not null)
        {
            return Microsoft.AspNetCore.Http.Results.ValidationProblem(
                error.ValidationErrors.ToDictionary(kv => kv.Key, kv => kv.Value),
                title: title);
        }

        return Microsoft.AspNetCore.Http.Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: statusCode,
            type: $"https://httpstatuses.io/{statusCode}");
    }
}
