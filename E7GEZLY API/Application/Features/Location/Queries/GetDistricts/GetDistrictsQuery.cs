using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Location;
using MediatR;

namespace E7GEZLY_API.Application.Features.Location.Queries.GetDistricts
{
    /// <summary>
    /// Query to get districts by governorate
    /// </summary>
    public record GetDistrictsQuery(int? GovernorateId = null) : IRequest<ApplicationResult<IEnumerable<DistrictDto>>>;
}