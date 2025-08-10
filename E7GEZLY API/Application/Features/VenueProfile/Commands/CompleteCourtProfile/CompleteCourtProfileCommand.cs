using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompleteCourtProfile
{
    /// <summary>
    /// Command for completing court venue profiles (Football/Padel)
    /// </summary>
    public class CompleteCourtProfileCommand : IRequest<ApplicationResult<VenueProfileCompletionResponseDto>>
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
        
        // Court-specific timing
        public TimeSpan MorningStartTime { get; init; }
        public TimeSpan MorningEndTime { get; init; }
        public TimeSpan EveningStartTime { get; init; }
        public TimeSpan EveningEndTime { get; init; }
        
        // Pricing
        public decimal MorningHourPrice { get; init; }
        public decimal EveningHourPrice { get; init; }
        public decimal DepositPercentage { get; init; } = 25;
        
        // Images
        public List<string>? ImageUrls { get; init; }
    }
}