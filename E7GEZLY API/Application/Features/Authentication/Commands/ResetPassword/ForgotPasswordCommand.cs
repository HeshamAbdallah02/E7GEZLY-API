using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Command for initiating password reset process (forgot password)
    /// </summary>
    public class ForgotPasswordCommand : IRequest<ApplicationResult<PasswordResetResponseDto>>
    {
        public string Identifier { get; init; } = string.Empty; // Email or phone
        public UserType UserType { get; init; }
    }
}