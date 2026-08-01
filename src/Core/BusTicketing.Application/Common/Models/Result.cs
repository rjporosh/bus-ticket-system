namespace BusTicketing.Application.Common.Models;

/// <summary>Machine-readable error category, used by the API layer to pick an HTTP status code.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Unexpected
}

public sealed record Error(string Code, string Message, ErrorType Type, IDictionary<string, string[]>? ValidationErrors = null)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);

    public static Error NotFound(string message) => new("NotFound", message, ErrorType.NotFound);
    public static Error Conflict(string message) => new("Conflict", message, ErrorType.Conflict);
    public static Error Unauthorized(string message) => new("Unauthorized", message, ErrorType.Unauthorized);
    public static Error Forbidden(string message) => new("Forbidden", message, ErrorType.Forbidden);
    public static Error Unexpected(string message) => new("Unexpected", message, ErrorType.Unexpected);

    public static Error Validation(IDictionary<string, string[]> errors) =>
        new("Validation", "One or more validation errors occurred.", ErrorType.Validation, errors);
}

/// <summary>
/// Explicit success/failure wrapper for command and query handlers. Application code
/// returns a Result instead of throwing for expected/anticipated failures (not-found,
/// validation, conflicts); exceptions are reserved for truly unexpected failures, which
/// the global exception middleware turns into HTTP 500s.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot contain an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>Throws if accessed on a failed result — callers must check IsSuccess first.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
