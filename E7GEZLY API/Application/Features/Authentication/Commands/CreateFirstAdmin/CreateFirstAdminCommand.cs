using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.CreateFirstAdmin
{
    /// <summary>
    /// Command for creating the first admin sub-user after venue profile completion
    /// </summary>
    public class CreateFirstAdminCommand : IRequest<ApplicationResult<CreateFirstAdminResponseDto>>
    {
        public Guid VenueId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for creating first admin
    /// </summary>
    public class CreateFirstAdminResponseDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public VenueSubUserResponseDto? SubUser { get; init; }
        public string NextStep { get; init; } = string.Empty;
    }
}