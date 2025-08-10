using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.Register
{
    /// <summary>
    /// Command for registering a new venue user
    /// </summary>
    public class RegisterVenueCommand : IRequest<ApplicationResult<VenueRegistrationResponseDto>>
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string VenueName { get; init; } = string.Empty;
        public VenueType VenueType { get; init; }
    }

    /// <summary>
    /// Response DTO for venue registration
    /// </summary>
    public class VenueRegistrationResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public Guid VenueId { get; init; }
        public bool RequiresVerification { get; init; }
        public bool RequiresProfileCompletion { get; init; }
        public string? VerificationCode { get; init; } // For development only
    }
}