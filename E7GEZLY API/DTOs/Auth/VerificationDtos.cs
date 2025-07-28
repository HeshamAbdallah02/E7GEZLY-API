// DTOs/Auth/VerificationDtos.cs
using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.DTOs.Auth
{
    public enum VerificationPurpose
    {
        AccountVerification,
        PasswordReset
    }
    public enum VerificationMethod
    {
        Phone,
        Email
    }

    public record SendVerificationCodeDto(
        [Required] string UserId,
        [Required] VerificationMethod Method,
        VerificationPurpose Purpose = VerificationPurpose.AccountVerification
    );

    public record VerifyAccountDto(
        [Required] string UserId,
        [Required][StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")] string VerificationCode,
        [Required] VerificationMethod Method
    );

    public record VerificationResponseDto(
        bool Success,
        string Message,
        string? VerificationCode = null);

    public record RegistrationResponseDto(
        bool Success,
        string Message,
        string? UserId = null,
        bool RequiresVerification = false
    );

    public record SendEmailVerificationDto
    {
        [Required]
        public string UserId { get; init; } = string.Empty;
    }
}