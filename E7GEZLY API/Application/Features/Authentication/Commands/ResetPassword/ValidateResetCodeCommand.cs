using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Command for validating password reset code
    /// </summary>
    public class ValidateResetCodeCommand : IRequest<ApplicationResult<ValidateResetCodeResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
        public string ResetCode { get; init; } = string.Empty;
        public ResetMethod Method { get; init; }
    }

    /// <summary>
    /// Response DTO for reset code validation
    /// </summary>
    public class ValidateResetCodeResponseDto
    {
        public bool IsValid { get; init; }
        public string Message { get; init; } = string.Empty;
        public double? ExpiresInMinutes { get; init; }
    }
}