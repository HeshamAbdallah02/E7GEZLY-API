using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Domain.Enums;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.UpdateSubUser
{
    /// <summary>
    /// Command for updating a venue sub-user
    /// </summary>
    public class UpdateSubUserCommand : IRequest<ApplicationResult<VenueSubUserResponseDto>>
    {
        public Guid Id { get; init; }
        public Guid VenueId { get; init; }
        public string? Username { get; init; }
        public VenueSubUserRole? Role { get; init; }
        public VenuePermissions? Permissions { get; init; }
        public bool IsActive { get; init; } = true;
    }
}