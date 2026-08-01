namespace BusTicketing.Domain.Exceptions;

/// <summary>Thrown when a domain invariant is violated inside an entity or aggregate.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Thrown when an entity cannot be located by its identifier.</summary>
public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" with key ({key}) was not found.")
    {
    }
}

/// <summary>
/// Thrown when a business rule prevents an operation that would otherwise succeed
/// technically (e.g. selling an already-sold seat). Mapped to HTTP 409 Conflict.
/// </summary>
public class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message) : base(message) { }
}
