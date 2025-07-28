// DTOs/Auth/PasswordResetDtos.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.DTOs.Auth
{
    public enum ResetMethod
    {
        Phone = 0,
        Email = 1
    }

    public record ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email or phone number is required")]
        public string Identifier { get; init; } = string.Empty; // Email or phone number

        [Required(ErrorMessage = "User type is required")]
        public UserType UserType { get; init; } // Customer or Venue
    }

    public record ResetPasswordDto
    {
        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; init; } = string.Empty;

        [Required(ErrorMessage = "Reset code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Reset code must be 6 digits")]
        public string ResetCode { get; init; } = string.Empty;

        [Required(ErrorMessage = "Method is required")]
        public ResetMethod Method { get; init; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string NewPassword { get; init; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }

    public record ValidateResetCodeDto
    {
        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; init; } = string.Empty;

        [Required(ErrorMessage = "Reset code is required")]
        public string ResetCode { get; init; } = string.Empty;

        [Required(ErrorMessage = "Method is required")]
        public ResetMethod Method { get; init; }
    }

    public record PasswordResetResponseDto(
        bool Success,
        string Message,
        string? UserId = null,
        string? ResetCode = null,
        bool? RequiresVerification = null  // Add this
    );

    public enum UserType
    {
        Customer = 0,
        Venue = 1
    }
}