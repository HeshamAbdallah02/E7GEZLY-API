using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SendEmailVerification
{
    /// <summary>
    /// Command for sending email verification code
    /// </summary>
    public class SendEmailVerificationCommand : IRequest<ApplicationResult<SendEmailVerificationResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for send email verification
    /// </summary>
    public class SendEmailVerificationResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int ExpiresInMinutes { get; init; } = 10;
        public string? VerificationCode { get; init; } // Only for development
    }
}