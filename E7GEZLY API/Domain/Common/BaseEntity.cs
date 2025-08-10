namespace E7GEZLY_API.Domain.Common;

/// <summary>
/// Base class for all domain entities with common properties
/// Provides identity, audit tracking, and soft delete capabilities
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void MarkAsDeleted()
    {
        if (IsDeleted) return;
        
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new EntityDeletedEvent(Id, GetType().Name));
    }

    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    // Protected methods for entity reconstruction from persistence
    protected void SetId(Guid id)
    {
        Id = id;
    }

    protected void SetCreatedAt(DateTime createdAt)
    {
        CreatedAt = createdAt;
    }

    protected void SetUpdatedAt(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
    }

    // Public method for external services to update timestamp (used by services layer)
    public void SetUpdatedAtExternal(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
    }

    public override bool Equals(object? obj)
    {
        return obj is BaseEntity other && Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(BaseEntity? left, BaseEntity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BaseEntity? left, BaseEntity? right)
    {
        return !Equals(left, right);
    }
}