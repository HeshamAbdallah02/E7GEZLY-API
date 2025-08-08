using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.VenueProfile.Queries.GetVenueProfile
{
    /// <summary>
    /// Handler for GetVenueProfileQuery
    /// </summary>
    public class GetVenueProfileHandler : IRequestHandler<GetVenueProfileQuery, ApplicationResult<VenueProfileDto>>
    {
        private readonly IVenueRepository _venueRepository;
        private readonly ILogger<GetVenueProfileHandler> _logger;

        public GetVenueProfileHandler(
            IVenueRepository venueRepository,
            ILogger<GetVenueProfileHandler> logger)
        {
            _venueRepository = venueRepository;
            _logger = logger;
        }

        public async Task<ApplicationResult<VenueProfileDto>> Handle(GetVenueProfileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Get venue from domain repository
                var venue = await _venueRepository.GetByIdAsync(request.VenueId, cancellationToken);
                
                if (venue == null)
                {
                    return ApplicationResult<VenueProfileDto>.Failure($"Venue with ID {request.VenueId} not found");
                }

                // Build working hours
                var workingHours = venue.WorkingHours?.Select(wh => new VenueWorkingHoursDto
                {
                    Id = wh.Id,
                    DayOfWeek = wh.DayOfWeek,
                    OpenTime = wh.OpenTime,
                    CloseTime = wh.CloseTime,
                    IsActive = wh.IsActive,
                    IsClosed = wh.IsClosed,
                    MorningStartTime = null,
                    MorningEndTime = null,
                    EveningStartTime = null,
                    EveningEndTime = null
                }).ToList();

                // Build pricing
                var pricing = venue.Pricing?.Select(p => new VenuePricingDto
                {
                    Id = p.Id,
                    Name = p.Name ?? string.Empty,
                    PricePerHour = p.PricePerHour,
                    Price = p.Price,
                    Description = p.Description,
                    IsActive = p.IsActive,
                    Type = p.Type,
                    PlayStationModel = p.PlayStationModel,
                    RoomType = p.RoomType,
                    GameMode = p.GameMode,
                    TimeSlotType = p.TimeSlotType,
                    DepositPercentage = p.DepositPercentage
                }).ToList();

                // Build image URLs
                var imageUrls = venue.Images?.OrderBy(img => img.DisplayOrder)
                    .Select(img => img.ImageUrl).ToList();

                // Build location
                AddressResponseDto? location = null;
                if (venue.Address != null)
                {
                    location = new AddressResponseDto(
                        Latitude: venue.Address.Coordinates?.Latitude,
                        Longitude: venue.Address.Coordinates?.Longitude,
                        StreetAddress: venue.Address.StreetAddress,
                        Landmark: venue.Address.Landmark,
                        District: null, // Will need to be populated from district lookup
                        DistrictAr: null,
                        Governorate: null,
                        GovernorateAr: null,
                        FullAddress: venue.Address.ToString()
                    );
                }

                // Build PlayStation details
                VenuePlayStationDetailsDto? playStationDetails = null;
                if (venue.PlayStationDetails != null)
                {
                    playStationDetails = new VenuePlayStationDetailsDto(
                        NumberOfRooms: venue.PlayStationDetails.NumberOfRooms,
                        HasPS4: venue.PlayStationDetails.HasPS4,
                        HasPS5: venue.PlayStationDetails.HasPS5,
                        HasVIPRooms: venue.PlayStationDetails.HasVIPRooms,
                        HasCafe: venue.PlayStationDetails.HasCafe,
                        HasWiFi: venue.PlayStationDetails.HasWiFi,
                        ShowsMatches: venue.PlayStationDetails.ShowsMatches
                    );
                }

                // Create the complete venue profile DTO
                var venueProfile = new VenueProfileDto
                {
                    Id = venue.Id,
                    Name = venue.Name.Value,
                    VenueType = venue.VenueType.ToString(),
                    Type = venue.VenueType.ToString(),
                    TypeValue = (int)venue.VenueType,
                    Description = venue.Description,
                    Address = venue.Address?.StreetAddress,
                    City = venue.City,
                    Governorate = venue.Governorate,
                    Latitude = venue.Address?.Coordinates?.Latitude,
                    Longitude = venue.Address?.Coordinates?.Longitude,
                    IsProfileComplete = venue.IsProfileComplete,
                    IsActive = venue.IsActive,
                    CreatedAt = venue.CreatedAt,
                    UpdatedAt = venue.UpdatedAt,
                    Location = location,
                    WorkingHours = workingHours,
                    Pricings = pricing,
                    Pricing = pricing,
                    ImageUrls = imageUrls,
                    PlayStationDetails = playStationDetails
                };

                return ApplicationResult<VenueProfileDto>.Success(venueProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting venue profile for venue {request.VenueId}");
                return ApplicationResult<VenueProfileDto>.Failure("An error occurred while retrieving the venue profile");
            }
        }
    }
}