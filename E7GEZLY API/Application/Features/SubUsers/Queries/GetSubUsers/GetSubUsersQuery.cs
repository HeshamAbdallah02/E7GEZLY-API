using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Queries.GetSubUsers
{
    /// <summary>
    /// Query for getting all sub-users for a venue
    /// </summary>
    public class GetSubUsersQuery : IRequest<ApplicationResult<IEnumerable<VenueSubUserResponseDto>>>
    {
        public Guid VenueId { get; init; }
    }
}