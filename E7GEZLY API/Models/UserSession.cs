// Models/UserSession.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    public class UserSession : BaseSyncEntity
    {
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string RefreshToken { get; set; } = null!;

        public DateTime RefreshTokenExpiry { get; set; }

        [StringLength(200)]
        public string? DeviceName { get; set; }

        [StringLength(200)]
        public string? DeviceType { get; set; } // Mobile, Web, Desktop

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        public DateTime LastActivityAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Optional: Location info
        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }
    }
}