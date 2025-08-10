using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Command for confirming password reset with code
    /// </summary>
    public class ConfirmPasswordResetCommand : IRequest<ApplicationResult<PasswordResetConfirmResponseDto>>
    {
        public string EmailOrPhone { get; init; } = string.Empty;
        public string ResetCode { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for password reset confirmation
    /// </summary>
    public class PasswordResetConfirmResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public bool RequiresLogin { get; init; } = true;
    }
}