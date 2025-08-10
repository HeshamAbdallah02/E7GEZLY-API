using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.VenueProfile.Queries.GetVenueProfile
{
    /// <summary>
    /// Query for getting venue profile information
    /// </summary>
    public class GetVenueProfileQuery : IRequest<ApplicationResult<VenueProfileDto>>
    {
        public Guid VenueId { get; init; }
        public bool IncludeInactiveElements { get; init; } = false;
    }
}