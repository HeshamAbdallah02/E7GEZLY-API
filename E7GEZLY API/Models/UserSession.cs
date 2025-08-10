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

        // Logout tracking
        public DateTime? LogoutAt { get; set; }
        [StringLength(200)]
        public string? LogoutReason { get; set; }

        // Optional: Location info
        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        // Computed properties for backward compatibility
        public DateTime ExpiresAt => RefreshTokenExpiry;
        public string? DeviceInfo => $"{DeviceName} ({DeviceType})".Trim(' ', '(', ')');

        // Factory method for repository compatibility
        public static UserSession CreateExisting(
            string userId,
            string refreshToken,
            DateTime expiry,
            string? deviceName = null,
            string? deviceType = null,
            string? userAgent = null,
            string? ipAddress = null)
        {
            return new UserSession
            {
                UserId = userId,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = expiry,
                DeviceName = deviceName,
                DeviceType = deviceType,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}