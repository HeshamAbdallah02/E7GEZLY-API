using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Testing.Queries.TestGeocoding
{
    /// <summary>
    /// Query for testing geocoding functionality
    /// </summary>
    public record TestGeocodingQuery(double Latitude, double Longitude) : IRequest<ApplicationResult<GeocodingTestResponse>>;

    /// <summary>
    /// Response for geocoding test
    /// </summary>
    public record GeocodingTestResponse
    {
        public CoordinateInfo Coordinates { get; init; } = null!;
        public object? AddressInfo { get; init; }
        public int? DistrictId { get; init; }
        public DistrictTestInfo? DistrictDetails { get; init; }
        public bool Success { get; init; }
    }

    /// <summary>
    /// Coordinate information
    /// </summary>
    public record CoordinateInfo(double Latitude, double Longitude);

    /// <summary>
    /// District test information
    /// </summary>
    public record DistrictTestInfo
    {
        public int Id { get; init; }
        public string NameEn { get; init; } = string.Empty;
        public string NameAr { get; init; } = string.Empty;
        public string Governorate { get; init; } = string.Empty;
    }
}