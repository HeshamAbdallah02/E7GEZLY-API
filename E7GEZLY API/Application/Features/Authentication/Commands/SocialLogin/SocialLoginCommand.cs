using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SocialLogin
{
    /// <summary>
    /// Command for social media authentication
    /// </summary>
    public class SocialLoginCommand : IRequest<OperationResult<AuthResponseDto>>
    {
        public string Provider { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
    }
}