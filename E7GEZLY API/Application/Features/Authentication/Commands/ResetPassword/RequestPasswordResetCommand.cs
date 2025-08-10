using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Command for requesting password reset
    /// </summary>
    public class RequestPasswordResetCommand : IRequest<ApplicationResult<PasswordResetRequestResponseDto>>
    {
        public string EmailOrPhone { get; init; } = string.Empty;
        public string ResetMethod { get; init; } = "Phone"; // Phone or Email
    }

    /// <summary>
    /// Response DTO for password reset request
    /// </summary>
    public class PasswordResetRequestResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string ResetMethod { get; init; } = string.Empty;
        public string MaskedContact { get; init; } = string.Empty;
        public string? VerificationCode { get; init; } // For development only
    }
}