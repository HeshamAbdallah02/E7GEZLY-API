using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Location;
using MediatR;

namespace E7GEZLY_API.Application.Features.Location.Queries.GetGovernorates
{
    /// <summary>
    /// Query to get all governorates
    /// </summary>
    public record GetGovernoratesQuery : IRequest<ApplicationResult<IEnumerable<GovernorateDto>>>;
}