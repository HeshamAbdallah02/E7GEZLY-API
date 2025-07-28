// DTOs/Auth/AccountManagementDtos.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.DTOs.Auth
{
    public record ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; init; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string NewPassword { get; init; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; init; } = string.Empty;

        public bool LogoutAllDevices { get; init; } = false;
    }

    public record DeactivateAccountDto
    {
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; init; } = string.Empty;

        [StringLength(500)]
        public string? Reason { get; init; }
    }

    public record UpdateEmailDto
    {
        [Required(ErrorMessage = "New email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string NewEmail { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; init; } = string.Empty;
    }

    public record UpdatePhoneDto
    {
        [Required(ErrorMessage = "New phone number is required")]
        [RegularExpression(@"^01[0125]\d{8}$", ErrorMessage = "Phone number must be in format 01xxxxxxxxx")]
        public string NewPhoneNumber { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; init; } = string.Empty;
    }
}