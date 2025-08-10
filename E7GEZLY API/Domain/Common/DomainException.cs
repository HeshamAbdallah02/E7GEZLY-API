namespace E7GEZLY_API.Domain.Common;

/// <summary>
/// Base class for all domain-specific exceptions
/// These represent business rule violations or invalid domain operations
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a domain entity is not found
/// </summary>
public sealed class DomainEntityNotFoundException : DomainException
{
    public DomainEntityNotFoundException(string entityType, Guid id) 
        : base($"{entityType} with ID '{id}' was not found.")
    {
        EntityType = entityType;
        EntityId = id;
    }

    public string EntityType { get; }
    public Guid EntityId { get; }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message)
    {
    }

    public BusinessRuleViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public BusinessRuleViolationException(string message, string property) : base(message)
    {
        Property = property;
    }

    public string? Property { get; }
}

/// <summary>
/// Exception thrown when an invalid operation is attempted on a domain object
/// </summary>
public sealed class InvalidDomainOperationException : DomainException
{
    public InvalidDomainOperationException(string operation, string reason)
        : base($"Invalid domain operation '{operation}': {reason}")
    {
        Operation = operation;
        Reason = reason;
    }

    public string Operation { get; }
    public string Reason { get; }
}