namespace E7GEZLY_API.Domain.Common;

/// <summary>
/// Marker interface for domain events
/// Domain events represent something important that happened in the domain
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
}

/// <summary>
/// Base implementation for domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
    }

    public Guid Id { get; init; }
    public DateTime OccurredAt { get; init; }
}

/// <summary>
/// Event raised when an entity is deleted
/// </summary>
public sealed record EntityDeletedEvent(Guid EntityId, string EntityType) : DomainEvent;