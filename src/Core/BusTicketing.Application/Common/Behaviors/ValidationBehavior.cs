using BusTicketing.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace BusTicketing.Application.Common.Behaviors;

/// <summary>
/// Runs all registered FluentValidation validators for the incoming request before the
/// handler executes. If the request's response type is Result/Result&lt;T&gt;, validation
/// failures are returned as a failed Result (no exception, no 500). Otherwise a
/// ValidationException is thrown for the global exception middleware to translate to 400.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errorsByProperty = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
                .MakeGenericMethod(resultType);

            return (TResponse)failureMethod.Invoke(null, new object[] { Error.Validation(errorsByProperty) })!;
        }

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(Error.Validation(errorsByProperty));

        throw new ValidationException(failures);
    }
}
