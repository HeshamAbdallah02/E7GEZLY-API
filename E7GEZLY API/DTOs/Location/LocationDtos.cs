using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.DTOs.Location
{
    public record GovernorateDto(
        int Id,
        string NameEn,
        string NameAr
    );

    public record DistrictDto
    {
        public int Id { get; init; }
        public string NameEn { get; init; } = string.Empty;
        public string NameAr { get; init; } = string.Empty;
        public int GovernorateId { get; init; }
        public string GovernorateName { get; init; } = string.Empty;
    }

    public record AddressDto
    {
        public string? Governorate { get; init; }
        public string? District { get; init; }
        public string? StreetAddress { get; init; }
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public string? Landmark { get; init; }
    }

    public record VenueAddressDto
    {
        [Required(ErrorMessage = "Map location is required")]
        [Range(-90, 90, ErrorMessage = "Invalid latitude")]
        public double Latitude { get; init; }

        [Required(ErrorMessage = "Map location is required")]
        [Range(-180, 180, ErrorMessage = "Invalid longitude")]
        public double Longitude { get; init; }

        //If frontend can determine district
        public int? DistrictId { get; init; }

        [Required(ErrorMessage = "Street address is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Street address must be between 5 and 500 characters")]
        public string StreetAddress { get; init; } = string.Empty;

        [StringLength(200)]
        public string? Landmark { get; init; }
    }

    public record ValidateAddressDto(
        int? DistrictId,
        double? Latitude,
        double? Longitude,
        string? StreetAddress,
        string? District,
        string? Governorate
    );

    public record AddressValidationResultDto(
        bool IsValid,
        string? Message,
        List<string>? Errors
    );

    /// <summary>
    /// Response for geocoding operations
    /// </summary>
    public record GeocodeResponseDto
    {
        public bool Success { get; init; }
        public AddressResponseDto Data { get; init; } = null!;
        public string Message { get; init; } = string.Empty;
    }
}