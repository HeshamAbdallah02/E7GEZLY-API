using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompleteProfile
{
    /// <summary>
    /// Command to complete venue profile using Clean Architecture
    /// </summary>
    public class CompleteVenueProfileCommand : IRequest<ApplicationResult<VenueProfileCompletionResponseDto>>
    {
        public Guid VenueId { get; set; }
        public string StreetAddress { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public int DistrictId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Description { get; set; }
        public List<VenueWorkingHoursDto> WorkingHours { get; set; } = new();
        public List<VenuePricingDto> Pricing { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        
        // For PlayStation venues
        public VenuePlayStationDetailsDto? PlayStationDetails { get; set; }
    }
}