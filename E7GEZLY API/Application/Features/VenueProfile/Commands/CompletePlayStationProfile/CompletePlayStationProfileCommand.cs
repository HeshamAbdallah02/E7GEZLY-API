using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompletePlayStationProfile
{
    /// <summary>
    /// Command for completing PlayStation venue profiles
    /// </summary>
    public class CompletePlayStationProfileCommand : IRequest<ApplicationResult<VenueProfileCompletionResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
        
        // Location info
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public int DistrictId { get; init; }
        public string? StreetAddress { get; init; }
        public string? Landmark { get; init; }
        
        // Working hours
        public List<WorkingHoursDto> WorkingHours { get; init; } = new();
        
        // PlayStation specific details
        public int NumberOfRooms { get; init; }
        public bool HasPS4 { get; init; }
        public bool HasPS5 { get; init; }
        public bool HasVIPRooms { get; init; }
        
        // Features
        public bool HasCafe { get; init; }
        public bool HasWiFi { get; init; }
        public bool ShowsMatches { get; init; }
        
        // Pricing
        public PlayStationPricingDto? PS4Pricing { get; init; }
        public PlayStationPricingDto? PS5Pricing { get; init; }
        
        // Images
        public List<string>? ImageUrls { get; init; }
    }
}