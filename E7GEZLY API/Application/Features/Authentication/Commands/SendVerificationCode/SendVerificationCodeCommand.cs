using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SendVerificationCode
{
    /// <summary>
    /// Command for sending verification codes via email or SMS
    /// </summary>
    public class SendVerificationCodeCommand : IRequest<ApplicationResult<SendVerificationCodeResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
        public VerificationMethod Method { get; init; }
        public VerificationPurpose Purpose { get; init; } = VerificationPurpose.AccountVerification;
    }

    /// <summary>
    /// Response DTO for send verification code
    /// </summary>
    public class SendVerificationCodeResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? VerificationCode { get; init; } // Only for development
    }
}