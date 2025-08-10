using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Queries.GetSubUser
{
    /// <summary>
    /// Query for getting a specific sub-user by ID
    /// </summary>
    public class GetSubUserQuery : IRequest<ApplicationResult<VenueSubUserResponseDto>>
    {
        public Guid Id { get; init; }
        public Guid VenueId { get; init; }
        public bool IncludeInactive { get; init; } = false;
    }
}