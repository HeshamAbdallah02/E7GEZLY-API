using E7GEZLY_API.Domain.Common;

namespace E7GEZLY_API.Domain.Events;

/// <summary>
/// Event raised when a new venue is registered
/// </summary>
public sealed record VenueRegisteredEvent(
    Guid VenueId,
    string VenueName,
    string UserEmail,
    DateTime RegisteredAt) : DomainEvent;

/// <summary>
/// Event raised when venue profile is completed
/// </summary>
public sealed record VenueProfileCompletedEvent(
    Guid VenueId,
    string VenueName,
    DateTime CompletedAt) : DomainEvent;

/// <summary>
/// Event raised when venue details are updated
/// </summary>
public sealed record VenueDetailsUpdatedEvent(
    Guid VenueId,
    string VenueName,
    Guid? UpdatedBySubUserId,
    DateTime UpdatedAt) : DomainEvent;

/// <summary>
/// Event raised when venue working hours are updated
/// </summary>
public sealed record VenueWorkingHoursUpdatedEvent(
    Guid VenueId,
    Guid? UpdatedBySubUserId,
    DateTime UpdatedAt) : DomainEvent;

/// <summary>
/// Event raised when venue pricing is updated
/// </summary>
public sealed record VenuePricingUpdatedEvent(
    Guid VenueId,
    Guid? UpdatedBySubUserId,
    DateTime UpdatedAt) : DomainEvent;

/// <summary>
/// Event raised when a venue sub-user is created
/// </summary>
public sealed record VenueSubUserCreatedEvent(
    Guid VenueId,
    Guid SubUserId,
    string Username,
    string Role,
    Guid? CreatedBySubUserId,
    DateTime CreatedAt) : DomainEvent;

/// <summary>
/// Event raised when a venue sub-user is updated
/// </summary>
public sealed record VenueSubUserUpdatedEvent(
    Guid VenueId,
    Guid SubUserId,
    string Username,
    Guid? UpdatedBySubUserId,
    DateTime UpdatedAt) : DomainEvent;

/// <summary>
/// Event raised when a venue sub-user is deactivated
/// </summary>
public sealed record VenueSubUserDeactivatedEvent(
    Guid VenueId,
    Guid SubUserId,
    string Username,
    Guid? DeactivatedBySubUserId,
    DateTime DeactivatedAt) : DomainEvent;

/// <summary>
/// Event raised when a venue sub-user password is changed
/// </summary>
public sealed record VenueSubUserPasswordChangedEvent(
    Guid VenueId,
    Guid SubUserId,
    string Username,
    bool WasReset,
    Guid? ChangedBySubUserId,
    DateTime ChangedAt) : DomainEvent;

/// <summary>
/// Event raised when a venue sub-user logs in
/// </summary>
public sealed record VenueSubUserLoggedInEvent(
    Guid VenueId,
    Guid SubUserId,
    string Username,
    string? DeviceType,
    string? IpAddress,
    DateTime LoggedInAt) : DomainEvent;

/// <summary>
/// Event raised when a venue sub-user login fails
/// </summary>
public sealed record VenueSubUserLoginFailedEvent(
    Guid VenueId,
    string Username,
    string Reason,
    string? IpAddress,
    DateTime AttemptedAt) : DomainEvent;