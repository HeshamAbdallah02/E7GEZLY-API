using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.LoginSubUser
{
    /// <summary>
    /// Command for authenticating a venue sub-user
    /// </summary>
    public class LoginSubUserCommand : IRequest<OperationResult<VenueSubUserLoginResponseDto>>
    {
        public Guid VenueId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        
        // Session information
        public string? DeviceName { get; init; }
        public string? DeviceType { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
    }
}