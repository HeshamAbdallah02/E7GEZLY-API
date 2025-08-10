using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Command for performing password reset with validation code
    /// </summary>
    public class PerformPasswordResetCommand : IRequest<ApplicationResult<PasswordResetResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
        public string ResetCode { get; init; } = string.Empty;
        public ResetMethod Method { get; init; }
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}