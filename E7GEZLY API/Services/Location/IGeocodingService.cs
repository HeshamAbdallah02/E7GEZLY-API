// Services/Location/IGeocodingService.cs
namespace E7GEZLY_API.Services.Location
{
    public interface IGeocodingService
    {
        Task<GeocodingResult?> GetAddressFromCoordinatesAsync(double latitude, double longitude);
        Task<int?> GetDistrictIdFromCoordinatesAsync(double latitude, double longitude);
    }

    public class GeocodingResult
    {
        public int? DistrictId { get; set; }
        public string? DistrictName { get; set; }
        public string? GovernorateName { get; set; }
        public string? FormattedAddress { get; set; }
        public string? StreetName { get; set; }
        public string? Suburb { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Neighbourhood { get; set; }
        public string? County { get; set; }
        public string? PostCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double MatchConfidence { get; set; } // 0 to 1
    }
}