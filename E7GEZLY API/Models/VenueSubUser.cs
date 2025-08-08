using System.ComponentModel.DataAnnotations;
using E7GEZLY_API.Domain.Enums;

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
}