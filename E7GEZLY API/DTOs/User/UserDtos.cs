// DTOs/User/UserDtos.cs
namespace E7GEZLY_API.DTOs.User
{
    public record UserInfoDto(
        string Id,
        string Email,
        string PhoneNumber,
        bool IsPhoneVerified,
        bool IsEmailVerified,
        bool IsActive,
        DateTime CreatedAt
    );

    public record UserSummaryDto(
        string Id,
        string Email,
        string PhoneNumber
    );

    /// <summary>
    /// DTO for user profile information
    /// </summary>
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsPhoneVerified { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneNumberVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<string> Roles { get; set; } = new();
        public DTOs.Venue.VenueProfileDto? Venue { get; set; }
    }
}