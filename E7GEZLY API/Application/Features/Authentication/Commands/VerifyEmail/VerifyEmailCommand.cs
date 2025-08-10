using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.VerifyEmail
{
    /// <summary>
    /// Command for verifying user email with verification code
    /// </summary>
    public class VerifyEmailCommand : IRequest<ApplicationResult<VerificationResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
        public string VerificationCode { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for email verification
    /// </summary>
    public class VerificationResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public bool IsVerified { get; init; }
    }
}