using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Domain.Services;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.Repositories;
using MediatR;

namespace E7GEZLY_API.Application.Features.VenueProfile.Queries.IsProfileComplete
{
    /// <summary>
    /// Handler for IsProfileCompleteQuery
    /// </summary>
    public class IsProfileCompleteHandler : IRequestHandler<IsProfileCompleteQuery, OperationResult<ProfileCompletionStatusDto>>
    {
        private readonly IVenueRepository _venueRepository;
        private readonly IVenueProfileCompletionService _completionService;
        private readonly ILogger<IsProfileCompleteHandler> _logger;

        public IsProfileCompleteHandler(
            IVenueRepository venueRepository,
            IVenueProfileCompletionService completionService,
            ILogger<IsProfileCompleteHandler> logger)
        {
            _venueRepository = venueRepository;
            _completionService = completionService;
            _logger = logger;
        }

        public async Task<OperationResult<ProfileCompletionStatusDto>> Handle(IsProfileCompleteQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var venue = await _venueRepository.GetByIdAsync(request.VenueId, cancellationToken);

                if (venue == null)
                {
                    return OperationResult<ProfileCompletionStatusDto>.Failure("Venue not found");
                }

                // Use the domain service to check completion status
                var completionStatus = await _completionService.CheckVenueProfileCompletionAsync(venue);

                var missingFields = new List<string>();
                var completedSections = new List<string>();
                var requiredSections = new List<string>();

                // Basic Information
                requiredSections.Add("Basic Information");
                if (string.IsNullOrEmpty(venue.Name.Name) || 
                    venue.Address.IsEmpty ||
                    venue.Address.Coordinates == null)
                {
                    if (string.IsNullOrEmpty(venue.Name.Name)) missingFields.Add("Venue Name");
                    if (venue.Address.IsEmpty) missingFields.Add("Address");
                    if (venue.Address.Coordinates == null) missingFields.Add("Location Coordinates");
                }
                else
                {
                    completedSections.Add("Basic Information");
                }

                // Working Hours
                requiredSections.Add("Working Hours");
                var workingHours = await _venueRepository.GetWorkingHoursAsync(venue.Id, cancellationToken);
                if (!workingHours.Any())
                {
                    missingFields.Add("Working Hours");
                }
                else
                {
                    completedSections.Add("Working Hours");
                }

                // Pricing
                requiredSections.Add("Pricing");
                var pricing = await _venueRepository.GetPricingAsync(venue.Id, cancellationToken);
                if (!pricing.Any())
                {
                    missingFields.Add("Pricing Information");
                }
                else
                {
                    completedSections.Add("Pricing");
                }

                // Images
                requiredSections.Add("Images");
                var images = await _venueRepository.GetImagesAsync(venue.Id, cancellationToken);
                if (!images.Any())
                {
                    missingFields.Add("Venue Images");
                }
                else
                {
                    completedSections.Add("Images");
                }

                // Venue-specific requirements
                if (venue.VenueType == VenueType.PlayStationVenue)
                {
                    requiredSections.Add("PlayStation Details");
                    var psDetails = await _venueRepository.GetPlayStationDetailsAsync(venue.Id, cancellationToken);
                    if (psDetails == null)
                    {
                        missingFields.Add("PlayStation Details");
                    }
                    else
                    {
                        completedSections.Add("PlayStation Details");
                    }
                }

                // Calculate completion percentage
                decimal completionPercentage = requiredSections.Count > 0 ? 
                    (decimal)completedSections.Count / requiredSections.Count * 100 : 0;

                var response = new ProfileCompletionStatusDto
                {
                    IsComplete = completionStatus.IsComplete,
                    MissingFields = missingFields,
                    CompletedSections = completedSections,
                    RequiredSections = requiredSections,
                    CompletionPercentage = Math.Round(completionPercentage, 1),
                    VenueType = venue.VenueType.ToString(),
                    Message = completionStatus.IsComplete ? 
                        "Profile is complete" : 
                        $"Profile is {Math.Round(completionPercentage, 1)}% complete. {missingFields.Count} field(s) required."
                };

                return OperationResult<ProfileCompletionStatusDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking profile completion for venue {request.VenueId}");
                return OperationResult<ProfileCompletionStatusDto>.Failure("An error occurred while checking profile completion");
            }
        }
    }
}