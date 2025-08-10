// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace E7GEZLY_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public Guid? VenueId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        
        // Add missing UserType property that repositories expect
        public UserType UserType { get; set; }

        // Verification - Phone
        public bool IsPhoneNumberVerified { get; set; } = false;
        public string? PhoneNumberVerificationCode { get; set; }
        public DateTime? PhoneNumberVerificationCodeExpiry { get; set; }

        // Verification - Email
        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationCodeExpiry { get; set; }

        // Navigation properties
        public virtual Venue? Venue { get; set; }
        public virtual CustomerProfile? CustomerProfile { get; set; }
        public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

        // Password Reset - Phone
        public string? PhonePasswordResetCode { get; set; }
        public DateTime? PhonePasswordResetCodeExpiry { get; set; }
        public bool? PhonePasswordResetCodeUsed { get; set; }

        // Password Reset - Email
        public string? EmailPasswordResetCode { get; set; }
        public DateTime? EmailPasswordResetCodeExpiry { get; set; }
        public bool? EmailPasswordResetCodeUsed { get; set; }

        // Rate limiting
        public DateTime? LastPasswordResetRequest { get; set; }

        // Generic password reset properties for backward compatibility
        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiry { get; set; }

        // Social Logins
        public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
    }

    /// <summary>
    /// User types in the E7GEZLY system - matches domain enum
    /// </summary>
    public enum UserType
    {
        Venue = 0,
        Customer = 1
    }
}