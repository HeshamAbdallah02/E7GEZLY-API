// DTOs/Venue/VenueProfileDtos.cs
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.User;

namespace E7GEZLY_API.DTOs.Venue
{
    public record VenueProfileResponseDto(
        string UserType,
        UserInfoDto User,
        VenueDetailsDto Venue
    );

    public record VenueDetailsDto(
        Guid Id,
        string Name,
        string Type,
        int TypeValue,
        string Features,
        int FeaturesValue,
        bool IsProfileComplete,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        AddressResponseDto? Location
    );

    public record VenueSummaryDto(
        Guid Id,
        string Name,
        string Type,
        bool IsProfileComplete
    );
}