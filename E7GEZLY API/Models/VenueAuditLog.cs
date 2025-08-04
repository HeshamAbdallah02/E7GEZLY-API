using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    /// <summary>
    /// Audit log for tracking all venue-related actions
    /// </summary>
    public class VenueAuditLog
    {
        public Guid Id { get; set; }

        [Required]
        public Guid VenueId { get; set; }
        public virtual Venue Venue { get; set; } = null!;

        public Guid? SubUserId { get; set; }
        public virtual VenueSubUser? SubUser { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string EntityId { get; set; } = null!;

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public string? AdditionalData { get; set; }
    }

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

        // Venue Management
        public const string VenueUpdated = "Venue.Updated";
        public const string VenuePricingUpdated = "Venue.PricingUpdated";
        public const string VenueWorkingHoursUpdated = "Venue.WorkingHoursUpdated";

        // Booking Management
        public const string BookingCreated = "Booking.Created";
        public const string BookingUpdated = "Booking.Updated";
        public const string BookingCancelled = "Booking.Cancelled";
    }
}