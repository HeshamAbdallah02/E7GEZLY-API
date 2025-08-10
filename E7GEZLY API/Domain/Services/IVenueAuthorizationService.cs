using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Domain service for venue authorization logic
/// Encapsulates complex authorization rules for venue operations
/// </summary>
public interface IVenueAuthorizationService
{
    /// <summary>
    /// Checks if a sub-user has permission to perform a specific action
    /// </summary>
    Task<AuthorizationResult> CheckPermissionAsync(VenueSubUser subUser, VenuePermissions requiredPermission, string action);
    
    /// <summary>
    /// Checks if a sub-user can manage another sub-user
    /// </summary>
    Task<AuthorizationResult> CanManageSubUserAsync(VenueSubUser manager, VenueSubUser target, string operation);
    
    /// <summary>
    /// Checks if a sub-user can access venue resources
    /// </summary>
    Task<AuthorizationResult> CanAccessVenueResourceAsync(VenueSubUser subUser, Guid venueId);
    
    /// <summary>
    /// Gets effective permissions for a sub-user
    /// </summary>
    Task<VenuePermissions> GetEffectivePermissionsAsync(VenueSubUser subUser);
    
    /// <summary>
    /// Validates permission assignment for a role
    /// </summary>
    Task<ValidationResult> ValidatePermissionsForRoleAsync(VenueSubUserRole role, VenuePermissions permissions);
}

/// <summary>
/// Result of an authorization check
/// </summary>
public sealed class AuthorizationResult
{
    private AuthorizationResult(bool isAuthorized, string reason)
    {
        IsAuthorized = isAuthorized;
        Reason = reason;
    }

    public static AuthorizationResult Success() => new(true, string.Empty);
    public static AuthorizationResult Failure(string reason) => new(false, reason);

    public bool IsAuthorized { get; }
    public string Reason { get; }
}

/// <summary>
/// Result of a validation check
/// </summary>
public sealed class ValidationResult
{
    private ValidationResult(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
    }

    public static ValidationResult Success() => new(true, Array.Empty<string>());
    public static ValidationResult Failure(IEnumerable<string> errors) => new(false, errors);
    public static ValidationResult Failure(string error) => new(false, new[] { error });

    public bool IsValid { get; }
    public IReadOnlyCollection<string> Errors { get; }
}