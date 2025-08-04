// E7GEZLY API/Models/VenueSubUserSession.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.Models
{
    /// <summary>
    /// Session tracking for venue sub-users
    /// </summary>
    public class VenueSubUserSession : BaseSyncEntity
    {
        [Required]
        public Guid SubUserId { get; set; }
        public virtual VenueSubUser SubUser { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string RefreshToken { get; set; } = null!;

        public DateTime RefreshTokenExpiry { get; set; }

        [StringLength(200)]
        public string? DeviceName { get; set; }

        [StringLength(100)]
        public string? DeviceType { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// JWT ID (jti claim) for token blacklisting when user logs out
        /// This allows us to invalidate specific access tokens immediately
        /// </summary>
        [StringLength(50)]
        public string? AccessTokenJti { get; set; }
    }
}