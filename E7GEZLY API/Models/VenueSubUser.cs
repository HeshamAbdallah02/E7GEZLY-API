using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    /// <summary>
    /// Represents sub-users (admins and coworkers) for venues
    /// </summary>
    public class VenueSubUser : BaseSyncEntity
    {
        [Required]
        public Guid VenueId { get; set; }
        public virtual Venue Venue { get; set; } = null!;

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public VenueSubUserRole Role { get; set; }

        public VenuePermissions Permissions { get; set; } = VenuePermissions.None;

        public bool IsActive { get; set; } = true;

        public bool IsFounderAdmin { get; set; } = false;

        public Guid? CreatedBySubUserId { get; set; }
        public virtual VenueSubUser? CreatedBy { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

        public bool MustChangePassword { get; set; } = false;

        // Navigation properties
        public virtual ICollection<VenueAuditLog> AuditLogs { get; set; } = new List<VenueAuditLog>();
    }

    public enum VenueSubUserRole
    {
        Admin = 0,
        Coworker = 1
    }

    [Flags]
    public enum VenuePermissions : long
    {
        None = 0,

        // Venue Management
        ViewVenueDetails = 1 << 0,        // 1
        EditVenueDetails = 1 << 1,        // 2
        ManagePricing = 1 << 2,           // 4
        ManageWorkingHours = 1 << 3,      // 8
        ManageVenueImages = 1 << 4,       // 16

        // Sub-User Management
        ViewSubUsers = 1 << 5,            // 32
        CreateSubUsers = 1 << 6,          // 64
        EditSubUsers = 1 << 7,            // 128
        DeleteSubUsers = 1 << 8,          // 256
        ResetSubUserPasswords = 1 << 9,   // 512

        // Booking Management
        ViewBookings = 1 << 10,           // 1024
        CreateBookings = 1 << 11,         // 2048
        EditBookings = 1 << 12,           // 4096
        CancelBookings = 1 << 13,         // 8192

        // Customer Management
        ViewCustomers = 1 << 14,          // 16384
        ManageCustomers = 1 << 15,        // 32768

        // Financial
        ViewFinancials = 1 << 16,         // 65536
        ManageFinancials = 1 << 17,       // 131072
        ProcessRefunds = 1 << 18,         // 262144

        // Reporting
        ViewReports = 1 << 19,            // 524288
        ExportReports = 1 << 20,          // 1048576

        // Tracking
        ViewAuditLogs = 1 << 21,          // 2097152
        ViewCoworkerActivity = 1 << 22,   // 4194304

        // Default permission sets
        AdminPermissions = ~None,         // All bits set = -1
        CoworkerPermissions = ViewVenueDetails | ViewBookings | CreateBookings |
                             EditBookings | ViewCustomers | ViewReports
    }
}