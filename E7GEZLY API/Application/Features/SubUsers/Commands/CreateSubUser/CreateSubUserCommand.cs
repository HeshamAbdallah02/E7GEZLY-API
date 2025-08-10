using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Commands.CreateSubUser
{
    /// <summary>
    /// Command for creating a venue sub-user
    /// </summary>
    public class CreateSubUserCommand : IRequest<ApplicationResult<VenueSubUserResponseDto>>
    {
        public Guid VenueId { get; init; }
        public Guid? CreatedBySubUserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public VenueSubUserRole Role { get; init; }
        public VenuePermissions Permissions { get; init; }
        public bool IsActive { get; init; } = true;
    }
}