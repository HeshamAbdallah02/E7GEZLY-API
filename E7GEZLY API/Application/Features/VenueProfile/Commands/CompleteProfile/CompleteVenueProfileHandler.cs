using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Domain.Services;
using E7GEZLY_API.Domain.ValueObjects;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.DTOs.Location;
using MediatR;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompleteProfile
{
    /// <summary>
    /// Handler for CompleteVenueProfileCommand using Clean Architecture with Domain layer
    /// </summary>
    public class CompleteVenueProfileHandler : IRequestHandler<CompleteVenueProfileCommand, ApplicationResult<VenueProfileCompletionResponseDto>>
    {
        private readonly IVenueRepository _venueRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IVenueProfileCompletionService _profileCompletionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CompleteVenueProfileHandler> _logger;

        public CompleteVenueProfileHandler(
            IVenueRepository venueRepository,
            ILocationRepository locationRepository,
            IVenueProfileCompletionService profileCompletionService,
            IUnitOfWork unitOfWork,
            ILogger<CompleteVenueProfileHandler> logger)
        {
            _venueRepository = venueRepository;
            _locationRepository = locationRepository;
            _profileCompletionService = profileCompletionService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApplicationResult<VenueProfileCompletionResponseDto>> Handle(CompleteVenueProfileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get venue from repository
                var venue = await _venueRepository.GetByIdAsync(request.VenueId);
                if (venue == null)
                {
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure("Venue not found");
                }

                // Check if profile is already complete
                if (venue.IsProfileComplete)
                {
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure("Venue profile is already complete");
                }

                // Validate district exists
                // TODO: Fix architecture mismatch between int and Guid for district IDs
                // For now, skip district validation to focus on core functionality
                // var district = await _locationRepository.GetDistrictByIdAsync(request.DistrictId);
                // if (district == null)
                // {
                //     return ApplicationResult<VenueProfileCompletionResponseDto>.Failure("Invalid district selected");
                // }

                // Create address value object with validation
                var address = Address.Create(
                    request.StreetAddress,
                    request.Landmark,
                    request.Latitude,
                    request.Longitude
                );

                // Update venue with profile data - Get District SystemId for legacy method
                var district = await _locationRepository.GetDistrictByIdAsync(request.DistrictId);
                if (district == null)
                {
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure("Invalid district ID");
                }
                UpdateVenueProfile(venue, request, address, district.SystemId);
                
                // Check if profile is now complete
                var completionResult = await _profileCompletionService.CheckVenueProfileCompletionAsync(venue);
                
                if (!completionResult.IsComplete)
                {
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure(
                        $"Profile completion failed. Missing: {string.Join(", ", completionResult.MissingRequirements)}");
                }

                // Update venue in repository
                await _venueRepository.UpdateAsync(venue);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"Venue profile completed successfully: {venue.Name.Value}");

                // Build response
                var venueDetails = new VenueDetailsDto(
                    Id: venue.Id,
                    Name: venue.Name.Value,
                    Type: venue.VenueType.ToString(),
                    TypeValue: (int)venue.VenueType,
                    IsProfileComplete: venue.IsProfileComplete,
                    CreatedAt: venue.CreatedAt,
                    UpdatedAt: venue.UpdatedAt,
                    Location: venue.Address != null ? new AddressResponseDto(
                        Latitude: venue.Address.Coordinates?.Latitude ?? 0,
                        Longitude: venue.Address.Coordinates?.Longitude ?? 0,
                        StreetAddress: venue.Address.StreetAddress ?? "",
                        Landmark: venue.Address.Landmark,
                        District: district?.NameEn,
                        DistrictAr: district?.NameAr,
                        Governorate: district?.Governorate?.NameEn,
                        GovernorateAr: district?.Governorate?.NameAr,
                        FullAddress: $"{venue.Address.StreetAddress}, {district?.NameEn}, {district?.Governorate?.NameEn}"
                    ) : null
                );

                var response = new VenueProfileCompletionResponseDto(
                    Message: "Venue profile completed successfully",
                    IsProfileComplete: venue.IsProfileComplete,
                    Venue: venueDetails
                );

                return ApplicationResult<VenueProfileCompletionResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing venue profile for venue {VenueId}", request.VenueId);
                return ApplicationResult<VenueProfileCompletionResponseDto>.Failure("An error occurred while completing the profile");
            }
        }

        private void UpdateVenueProfile(Venue venue, CompleteVenueProfileCommand request, Address address, int districtId)
        {
            // Update venue address using the correct method signature
            venue.UpdateAddress(
                streetAddress: address.StreetAddress,
                landmark: address.Landmark,
                latitude: address.Coordinates?.Latitude,
                longitude: address.Coordinates?.Longitude);
            
            // Update district separately
            venue.UpdateDistrict(districtId);
            
            // Update working hours using individual day methods
            foreach (var wh in request.WorkingHours)
            {
                venue.SetWorkingHours(
                    dayOfWeek: wh.DayOfWeek,
                    openTime: wh.OpenTime,
                    closeTime: wh.CloseTime,
                    isClosed: wh.IsClosed);
            }
            
            // TODO: Handle images - need to check what method exists for updating images
            // if (request.ImageUrls?.Any() == true)
            // {
            //     venue.UpdateImages(request.ImageUrls);
            // }
            
            // Mark profile as complete
            venue.MarkProfileAsComplete();
        }

        private VenueProfileData CreateVenueProfileData(CompleteVenueProfileCommand request, Address address)
        {
            // Convert DTOs to domain objects
            var workingHours = request.WorkingHours.Select(wh => new VenueWorkingHoursData
            {
                DayOfWeek = (DayOfWeek)wh.DayOfWeek,
                OpenTime = wh.OpenTime,
                CloseTime = wh.CloseTime,
                IsWorkingDay = !wh.IsClosed
            }).ToList();

            var pricing = request.Pricing.Select(p => new VenuePricingData
            {
                Type = p.Type.ToString(),
                Price = p.Price,
                DepositPercentage = p.DepositPercentage
            }).ToList();

            VenuePlayStationDetailsData? playStationDetails = null;
            if (request.PlayStationDetails != null)
            {
                var consoleTypes = new List<string>();
                if (request.PlayStationDetails.HasPS4) consoleTypes.Add("PS4");
                if (request.PlayStationDetails.HasPS5) consoleTypes.Add("PS5");
                
                playStationDetails = new VenuePlayStationDetailsData
                {
                    NumberOfConsoles = request.PlayStationDetails.NumberOfRooms,
                    ConsoleTypes = string.Join(", ", consoleTypes),
                    HasPrivateRooms = request.PlayStationDetails.HasVIPRooms,
                    NumberOfPrivateRooms = request.PlayStationDetails.HasVIPRooms ? request.PlayStationDetails.NumberOfRooms : 0,
                    HasSnacks = request.PlayStationDetails.HasCafe,
                    HasDrinks = request.PlayStationDetails.HasCafe
                };
            }

            return new VenueProfileData
            {
                Address = address,
                Description = request.Description,
                WorkingHours = workingHours,
                Pricing = pricing,
                ImageUrls = request.ImageUrls,
                PlayStationDetails = playStationDetails
            };
        }
    }

    // DTOs for response
    public class AddressDto
    {
        public string StreetAddress { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public Guid DistrictId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    // Domain data transfer objects for profile completion
    public class VenueProfileData
    {
        public Address Address { get; set; } = null!;
        public string? Description { get; set; }
        public List<VenueWorkingHoursData> WorkingHours { get; set; } = new();
        public List<VenuePricingData> Pricing { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        public VenuePlayStationDetailsData? PlayStationDetails { get; set; }
    }

    public class VenueWorkingHoursData
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public bool IsWorkingDay { get; set; }
    }

    public class VenuePricingData
    {
        public string Type { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DepositPercentage { get; set; }
    }

    public class VenuePlayStationDetailsData
    {
        public int NumberOfConsoles { get; set; }
        public string ConsoleTypes { get; set; } = string.Empty;
        public bool HasPrivateRooms { get; set; }
        public int? NumberOfPrivateRooms { get; set; }
        public bool HasSnacks { get; set; }
        public bool HasDrinks { get; set; }
    }
}