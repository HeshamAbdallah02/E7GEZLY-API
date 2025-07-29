// Services/Venue/IVenueProfileService.cs
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.Services.VenueManagement
{
    /// <summary>
    /// Service interface for venue profile management
    /// </summary>
    public interface IVenueProfileService
    {
        /// <summary>
        /// Complete profile for court venues (Football/Padel)
        /// </summary>
        Task<VenueProfileCompletionResponseDto> CompleteCourtProfileAsync(
            string userId,
            CompleteCourtProfileDto dto);

        /// <summary>
        /// Complete profile for PlayStation venues
        /// </summary>
        Task<VenueProfileCompletionResponseDto> CompletePlayStationProfileAsync(
            string userId,
            CompletePlayStationProfileDto dto);

        /// <summary>
        /// Get venue profile completion status
        /// </summary>
        Task<bool> IsVenueProfileCompleteAsync(Guid venueId);

        /// <summary>
        /// Validate if venue type matches the profile completion request
        /// </summary>
        Task<bool> ValidateVenueTypeAsync(string userId, VenueType expectedType);
    }
}