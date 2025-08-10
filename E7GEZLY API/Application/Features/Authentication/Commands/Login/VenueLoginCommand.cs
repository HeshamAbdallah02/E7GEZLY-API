using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.Login
{
    /// <summary>
    /// Command for venue login (gateway authentication)
    /// </summary>
    public class VenueLoginCommand : IRequest<ApplicationResult<VenueLoginResponseDto>>
    {
        public string EmailOrPhone { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? DeviceName { get; init; }
        public string? DeviceType { get; init; }
        public string? UserAgent { get; init; }
        public string? IpAddress { get; init; }
    }

    /// <summary>
    /// Response DTO for venue login
    /// </summary>
    public class VenueLoginResponseDto
    {
        public string GatewayToken { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        public bool RequiresSubUserSetup { get; init; }
        public UserAuthInfoDto User { get; init; } = null!;
        public VenueAuthInfoDto Venue { get; init; } = null!;
        public List<string> RequiredActions { get; init; } = new();
        public AuthMetadataDto? Metadata { get; init; }
        public string NextStep { get; init; } = string.Empty;
    }
}