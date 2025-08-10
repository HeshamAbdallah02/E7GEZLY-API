using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Location.Queries.GetDistrictFromCoordinates
{
    /// <summary>
    /// Query to get district from coordinates
    /// </summary>
    public record GetDistrictFromCoordinatesQuery(double Latitude, double Longitude) : IRequest<ApplicationResult<DistrictFromCoordinatesDto>>;
    
    /// <summary>
    /// Response DTO for district from coordinates
    /// </summary>
    public record DistrictFromCoordinatesDto
    {
        public bool Success { get; init; }
        public int DistrictId { get; init; }
        public DistrictDetailsDto District { get; init; } = null!;
    }
    
    /// <summary>
    /// District details DTO
    /// </summary>
    public record DistrictDetailsDto
    {
        public int Id { get; init; }
        public string NameEn { get; init; } = string.Empty;
        public string NameAr { get; init; } = string.Empty;
        public GovernorateDetailsDto Governorate { get; init; } = null!;
    }
    
    /// <summary>
    /// Governorate details DTO
    /// </summary>
    public record GovernorateDetailsDto
    {
        public int Id { get; init; }
        public string NameEn { get; init; } = string.Empty;
        public string NameAr { get; init; } = string.Empty;
    }
}