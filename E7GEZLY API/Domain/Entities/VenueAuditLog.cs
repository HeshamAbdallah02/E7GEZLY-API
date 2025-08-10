using E7GEZLY_API.Domain.Common;

namespace E7GEZLY_API.Domain.Entities;

/// <summary>
/// Domain entity for tracking all venue-related actions and changes
/// Provides comprehensive audit trail for venue operations
/// </summary>
public sealed class VenueAuditLog : BaseEntity
{
    private VenueAuditLog(Guid venueId, string action, string entityType, string entityId, 
        string? oldValues, string? newValues, Guid? subUserId, string? ipAddress, 
        string? userAgent, string? additionalData) : base()
    {
        VenueId = venueId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
        SubUserId = subUserId;
        Timestamp = DateTime.UtcNow;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        AdditionalData = additionalData;
    }

    public static VenueAuditLog Create(Guid venueId, string action, string entityType, string entityId,
        string? oldValues = null, string? newValues = null, Guid? subUserId = null, 
        string? ipAddress = null, string? userAgent = null, string? additionalData = null)
    {
        if (venueId == Guid.Empty)
            throw new BusinessRuleViolationException("Venue ID is required for audit log");

        if (string.IsNullOrWhiteSpace(action))
            throw new BusinessRuleViolationException("Action is required for audit log");

        if (action.Length > 100)
            throw new BusinessRuleViolationException("Action cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(entityType))
            throw new BusinessRuleViolationException("Entity type is required for audit log");

        if (entityType.Length > 100)
            throw new BusinessRuleViolationException("Entity type cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(entityId))
            throw new BusinessRuleViolationException("Entity ID is required for audit log");

        if (entityId.Length > 100)
            throw new BusinessRuleViolationException("Entity ID cannot exceed 100 characters");

        if (ipAddress?.Length > 45)
            throw new BusinessRuleViolationException("IP address cannot exceed 45 characters");

        if (userAgent?.Length > 500)
            throw new BusinessRuleViolationException("User agent cannot exceed 500 characters");

        return new VenueAuditLog(venueId, action, entityType, entityId, oldValues, newValues, 
            subUserId, ipAddress, userAgent, additionalData);
    }

    public Guid VenueId { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public string EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public Guid? SubUserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? AdditionalData { get; private set; }

    // Navigation properties
    public VenueSubUser? SubUser { get; private set; }

    public bool IsSystemAction => !SubUserId.HasValue;
    public bool IsUserAction => SubUserId.HasValue;

    public override string ToString()
    {
        var actor = IsSystemAction ? "System" : $"SubUser:{SubUserId}";
        return $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{actor}] {Action} on {EntityType}:{EntityId}";
    }
}

/// <summary>
/// Static class containing predefined audit action constants
/// Ensures consistency in audit logging across the domain
/// </summary>
public static class VenueAuditActions
{
    // Sub-User Management
    public const string SubUserCreated = "SubUser.Created";
    public const string SubUserUpdated = "SubUser.Updated";
    public const string SubUserDeleted = "SubUser.Deleted";
    public const string SubUserActivated = "SubUser.Activated";
    public const string SubUserDeactivated = "SubUser.Deactivated";
    public const string SubUserPasswordChanged = "SubUser.PasswordChanged";
    public const string SubUserPasswordReset = "SubUser.PasswordReset";
    public const string SubUserLogin = "SubUser.Login";
    public const string SubUserLogout = "SubUser.Logout";
    public const string SubUserLoginFailed = "SubUser.LoginFailed";
    public const string SubUserLocked = "SubUser.Locked";
    public const string SubUserUnlocked = "SubUser.Unlocked";

    // Venue Management
    public const string VenueCreated = "Venue.Created";
    public const string VenueUpdated = "Venue.Updated";
    public const string VenueProfileCompleted = "Venue.ProfileCompleted";
    public const string VenueAddressUpdated = "Venue.AddressUpdated";
    public const string VenueFeaturesUpdated = "Venue.FeaturesUpdated";

    // Venue Details Management
    public const string VenueWorkingHoursCreated = "VenueWorkingHours.Created";
    public const string VenueWorkingHoursUpdated = "VenueWorkingHours.Updated";
    public const string VenueWorkingHoursDeleted = "VenueWorkingHours.Deleted";
    
    public const string VenuePricingCreated = "VenuePricing.Created";
    public const string VenuePricingUpdated = "VenuePricing.Updated";
    public const string VenuePricingDeleted = "VenuePricing.Deleted";
    
    public const string VenueImageUploaded = "VenueImage.Uploaded";
    public const string VenueImageUpdated = "VenueImage.Updated";
    public const string VenueImageDeleted = "VenueImage.Deleted";
    public const string VenueImageSetAsPrimary = "VenueImage.SetAsPrimary";

    public const string PlayStationDetailsCreated = "PlayStationDetails.Created";
    public const string PlayStationDetailsUpdated = "PlayStationDetails.Updated";

    // Booking Management
    public const string BookingCreated = "Booking.Created";
    public const string BookingUpdated = "Booking.Updated";
    public const string BookingConfirmed = "Booking.Confirmed";
    public const string BookingCancelled = "Booking.Cancelled";
    public const string BookingRefunded = "Booking.Refunded";

    // Customer Management
    public const string CustomerCreated = "Customer.Created";
    public const string CustomerUpdated = "Customer.Updated";
    public const string CustomerBlocked = "Customer.Blocked";
    public const string CustomerUnblocked = "Customer.Unblocked";

    // Financial Operations
    public const string PaymentReceived = "Payment.Received";
    public const string PaymentRefunded = "Payment.Refunded";
    public const string DepositReceived = "Deposit.Received";
    public const string DepositRefunded = "Deposit.Refunded";

    // System Operations
    public const string SystemMaintenance = "System.Maintenance";
    public const string DataBackup = "System.DataBackup";
    public const string SecurityScan = "System.SecurityScan";

    // Validation method to ensure action is from predefined list
    public static bool IsValidAction(string action)
    {
        var validActions = typeof(VenueAuditActions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(v => v != null)
            .ToHashSet();

        return validActions.Contains(action);
    }

    // Get all available actions
    public static IEnumerable<string> GetAllActions()
    {
        return typeof(VenueAuditActions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(v => v != null)
            .Cast<string>()
            .OrderBy(a => a);
    }

    // Get actions by category
    public static IEnumerable<string> GetActionsByCategory(string category)
    {
        return GetAllActions()
            .Where(a => a.StartsWith($"{category}.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a);
    }
}