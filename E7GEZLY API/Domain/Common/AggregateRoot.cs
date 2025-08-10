namespace E7GEZLY_API.Domain.Common;

/// <summary>
/// Base class for aggregate roots in the domain
/// Aggregate roots are the only entities that can be directly accessed from outside the domain
/// They maintain consistency boundaries and coordinate changes within the aggregate
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }
}

/// <summary>
/// Interface for aggregate roots to support repository pattern
/// </summary>
public interface IAggregateRoot
{
    Guid Id { get; }
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}