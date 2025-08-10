using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.CustomerLogin
{
    /// <summary>
    /// Command for customer login
    /// </summary>
    public class CustomerLoginCommand : IRequest<ApplicationResult<object>>
    {
        public string EmailOrPhone { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        
        // Device information for session tracking
        public string? DeviceName { get; init; }
        public string? DeviceType { get; init; }
        public string? UserAgent { get; init; }
        public string? IpAddress { get; init; }
    }
}