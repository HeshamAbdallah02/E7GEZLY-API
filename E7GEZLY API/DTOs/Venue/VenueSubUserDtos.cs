using E7GEZLY_API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.DTOs.Venue
{
    /// <summary>
    /// DTO for creating a venue sub-user
    /// </summary>
    public record CreateVenueSubUserDto(
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, underscores and hyphens")]
        string Username,

        [Required]
        [StringLength(100, MinimumLength = 8)]
        string Password,

        [Required]
        VenueSubUserRole Role,

        VenuePermissions? Permissions = null
    );

    /// <summary>
    /// DTO for venue sub-user login
    /// </summary>
    public record VenueSubUserLoginDto(
        [Required]
        string Username,

        [Required]
        string Password,

        string? DeviceType = null,
        string? IpAddress = null
    );

    /// <summary>
    /// DTO for updating a venue sub-user
    /// </summary>
    public record UpdateVenueSubUserDto(
        VenueSubUserRole? Role = null,
        VenuePermissions? Permissions = null,
        bool? IsActive = null
    );

    /// <summary>
    /// DTO for changing sub-user password
    /// </summary>
    public record ChangeSubUserPasswordDto(
        [Required]
        string CurrentPassword,

        [Required]
        [StringLength(100, MinimumLength = 8)]
        string NewPassword
    );

    /// <summary>
    /// DTO for admin resetting sub-user password
    /// </summary>
    public record ResetSubUserPasswordDto(
        [Required]
        [StringLength(100, MinimumLength = 8)]
        string NewPassword,

        bool MustChangePassword = true
    );

    /// <summary>
    /// Response DTO for venue sub-user
    /// </summary>
    public record VenueSubUserResponseDto(
        Guid Id,
        string Username,
        VenueSubUserRole Role,
        VenuePermissions Permissions,
        bool IsActive,
        bool IsFounderAdmin,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        string? CreatedByUsername
    );

    /// <summary>
    /// Response DTO for sub-user login
    /// </summary>
    public record VenueSubUserLoginResponseDto(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt,
        VenueSubUserResponseDto SubUser,
        bool MustChangePassword
    );

    /// <summary>
    /// Response DTO for venue gateway login
    /// </summary>
    public record VenueGatewayLoginResponseDto(
        string GatewayToken,
        bool RequiresSubUserSetup,
        VenueDetailsDto Venue
    );
}