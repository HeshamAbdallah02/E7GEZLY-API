using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.RefreshToken
{
    /// <summary>
    /// Command for refreshing authentication token
    /// </summary>
    public class RefreshTokenCommand : IRequest<ApplicationResult<AuthResponseDto>>
    {
        public string RefreshToken { get; init; } = string.Empty;
        public string? DeviceName { get; init; }
        public string? DeviceType { get; init; }
        public string? UserAgent { get; init; }
        public string? IpAddress { get; init; }
    }
}