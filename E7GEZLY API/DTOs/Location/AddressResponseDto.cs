// DTOs/Location/AddressResponseDtos.cs
namespace E7GEZLY_API.DTOs.Location
{
    public record AddressResponseDto(
        double? Latitude,
        double? Longitude,
        string? StreetAddress,
        string? Landmark,
        string? District,
        string? DistrictAr,
        string? Governorate,
        string? GovernorateAr,
        string? FullAddress
    );

    public record SimpleAddressResponseDto(
        string? StreetAddress,
        string? District,
        string? Governorate
    );
}