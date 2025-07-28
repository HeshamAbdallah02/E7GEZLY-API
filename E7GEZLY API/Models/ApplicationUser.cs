// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace E7GEZLY_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public Guid? VenueId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

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

        // Social Logins
        public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
    }
}