using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.VerifyAccount
{
    /// <summary>
    /// Command for verifying user account with verification code (email or phone)
    /// </summary>
    public class VerifyAccountCommand : IRequest<ApplicationResult<VerifyAccountResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
        public string VerificationCode { get; init; } = string.Empty;
        public VerificationMethod Method { get; init; }
    }

    /// <summary>
    /// Response DTO for account verification
    /// </summary>
    public class VerifyAccountResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public AuthResponseDto? Tokens { get; init; }
        public VenueInfoDto? VenueInfo { get; init; }
        public List<string>? RequiredActions { get; init; }
        public AuthMetadataDto? Metadata { get; init; }
        public string? UserType { get; init; }
    }

    /// <summary>
    /// Venue information DTO for response
    /// </summary>
    public class VenueInfoDto
    {
        public Guid? VenueId { get; init; }
        public string? VenueName { get; init; }
        public string? VenueType { get; init; }
        public bool IsProfileComplete { get; init; }
    }
}