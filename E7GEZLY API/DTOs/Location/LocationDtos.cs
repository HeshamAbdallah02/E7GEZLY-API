using System.ComponentModel.DataAnnotations;

namespace E7GEZLY_API.DTOs.Location
{
    public record GovernorateDto(
        int Id,
        string NameEn,
        string NameAr
    );

    public record DistrictDto(
        int Id,
        string NameEn,
        string NameAr,
        int GovernorateId
    );

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
        string? Governorate,
        string? District,
        double? Latitude,
        double? Longitude
    );

    public record AddressValidationResultDto(
        bool IsValid,
        string? Message,
        List<string>? Errors
    );
}