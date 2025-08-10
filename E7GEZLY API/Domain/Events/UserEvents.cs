using E7GEZLY_API.Domain.Common;

namespace E7GEZLY_API.Domain.Events;

/// <summary>
/// Event raised when a new user is registered
/// </summary>
public sealed record UserRegisteredEvent(
    string UserId,
    string Email,
    string? PhoneNumber,
    string UserType, // "Venue" or "Customer"
    DateTime RegisteredAt) : DomainEvent;

/// <summary>
/// Event raised when user email is verified
/// </summary>
public sealed record UserEmailVerifiedEvent(
    string UserId,
    string Email,
    DateTime VerifiedAt) : DomainEvent;

/// <summary>
/// Event raised when user phone number is verified
/// </summary>
public sealed record UserPhoneVerifiedEvent(
    string UserId,
    string PhoneNumber,
    DateTime VerifiedAt) : DomainEvent;

/// <summary>
/// Event raised when user password is changed
/// </summary>
public sealed record UserPasswordChangedEvent(
    string UserId,
    string Email,
    bool WasReset,
    DateTime ChangedAt) : DomainEvent;

/// <summary>
/// Event raised when user is locked out due to failed login attempts
/// </summary>
public sealed record UserLockedOutEvent(
    string UserId,
    string Email,
    DateTime LockedOutUntil,
    int FailedAttempts) : DomainEvent;

/// <summary>
/// Event raised when customer profile is created or updated
/// </summary>
public sealed record CustomerProfileUpdatedEvent(
    string UserId,
    string FirstName,
    string LastName,
    DateTime UpdatedAt) : DomainEvent;

/// <summary>
/// Event raised when a new user session is created
/// </summary>
public sealed record UserSessionCreatedEvent(
    Guid SessionId,
    string UserId,
    string? DeviceName,
    string? DeviceType,
    DateTime CreatedAt) : DomainEvent;

/// <summary>
/// Event raised when a user session refresh token is updated
/// </summary>
public sealed record UserSessionRefreshedEvent(
    Guid SessionId,
    string UserId,
    DateTime RefreshedAt) : DomainEvent;

/// <summary>
/// Event raised when a user session is deactivated/logged out
/// </summary>
public sealed record UserSessionDeactivatedEvent(
    Guid SessionId,
    string UserId,
    string Reason,
    DateTime DeactivatedAt) : DomainEvent;

/// <summary>
/// Event raised when an external login is linked to a user account
/// </summary>
public sealed record ExternalLoginLinkedEvent(
    string UserId,
    string LoginProvider,
    string ProviderKey,
    DateTime LinkedAt) : DomainEvent;

/// <summary>
/// Event raised when an external login is used for authentication
/// </summary>
public sealed record ExternalLoginUsedEvent(
    string UserId,
    string LoginProvider,
    DateTime UsedAt) : DomainEvent;

/// <summary>
/// Event raised when an external login is unlinked from a user account
/// </summary>
public sealed record ExternalLoginUnlinkedEvent(
    string UserId,
    string LoginProvider,
    string ProviderKey,
    DateTime UnlinkedAt) : DomainEvent;