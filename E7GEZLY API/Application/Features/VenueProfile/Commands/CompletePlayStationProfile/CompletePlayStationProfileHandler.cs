using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.Services;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompletePlayStationProfile
{
    /// <summary>
    /// Handler for CompletePlayStationProfileCommand
    /// </summary>
    public class CompletePlayStationProfileHandler : IRequestHandler<CompletePlayStationProfileCommand, ApplicationResult<VenueProfileCompletionResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly IVenueProfileCompletionService _profileCompletionService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<CompletePlayStationProfileHandler> _logger;

        public CompletePlayStationProfileHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            IVenueProfileCompletionService profileCompletionService,
            IDateTimeService dateTimeService,
            ILogger<CompletePlayStationProfileHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _profileCompletionService = profileCompletionService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<VenueProfileCompletionResponseDto>> Handle(CompletePlayStationProfileCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await ((AppDbContext)_context).Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                var user = await GetUserWithVenueAsync(request.UserId);
                if (user?.Venue == null)
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure("Venue not found for user");

                var venue = user.Venue;

                // Check if profile is already complete
                if (venue.IsProfileComplete)
                {
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure(
                        "Venue profile is already complete. Use the update endpoint to modify profile.");
                }

                // Validate venue type using domain service
                var domainVenueType = (Domain.Enums.VenueType)venue.VenueType;
                var isValidVenueType = _profileCompletionService.CanCompletePlayStationProfile(domainVenueType);
                if (!isValidVenueType)
                {
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure(
                        $"This endpoint is for PlayStation venues only. Venue type is {venue.VenueType}");
                }

                // Validate PlayStation models
                if (!request.HasPS4 && !request.HasPS5)
                {
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure(
                        "Venue must have at least PS4 or PS5");
                }

                // Update location
                await UpdateVenueLocationAsync(venue, request);

                // Add working hours
                await AddWorkingHoursAsync(venue, request.WorkingHours);

                // Add PlayStation details
                await AddPlayStationDetailsAsync(venue, request);

                // Add pricing
                await AddPlayStationPricingAsync(venue, request);

                // Add images if provided
                if (request.ImageUrls?.Any() == true)
                {
                    await AddVenueImagesAsync(venue, request.ImageUrls);
                }

                // Mark profile as complete
                venue.IsProfileComplete = true;
                venue.UpdatedAt = _dateTimeService.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"PlayStation venue profile completed for venue {venue.Id}");

                var response = new VenueProfileCompletionResponseDto(
                    "Venue profile completed successfully",
                    true,
                    MapToVenueDetailsDto(venue)
                );

                return ApplicationResult<VenueProfileCompletionResponseDto>.Success(response);
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, $"Error completing PlayStation profile for user {request.UserId}");
                return ApplicationResult<VenueProfileCompletionResponseDto>.Failure("An error occurred while completing the profile");
            }
        }

        private async Task<ApplicationUser?> GetUserWithVenueAsync(string userId)
        {
            return await _userManager.Users
                .Include(u => u.Venue)
                .ThenInclude(v => v!.District)
                .ThenInclude(d => d!.Governorate)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        private async Task UpdateVenueLocationAsync(Venue venue, CompletePlayStationProfileCommand request)
        {
            venue.Latitude = request.Latitude;
            venue.Longitude = request.Longitude;
            venue.DistrictId = request.DistrictId;
            venue.StreetAddress = request.StreetAddress;
            venue.Landmark = request.Landmark;

            // Load district for full address computation if needed
            if (venue.District?.Governorate == null)
            {
                venue.District = await _context.Districts
                    .Include(d => d.Governorate)
                    .FirstOrDefaultAsync(d => d.Id == request.DistrictId);
            }
        }

        private async Task AddWorkingHoursAsync(Venue venue, List<WorkingHoursDto> workingHours)
        {
            // Remove existing working hours
            var existingHours = _context.VenueWorkingHours.Where(wh => wh.VenueId == venue.Id);
            foreach (var existing in existingHours)
            {
                _context.VenueWorkingHours.Remove(existing);
            }

            foreach (var wh in workingHours)
            {
                var workingHour = new VenueWorkingHours
                {
                    VenueId = venue.Id,
                    DayOfWeek = wh.DayOfWeek,
                    IsClosed = wh.IsClosed,
                    OpenTime = wh.OpenTime ?? TimeSpan.Zero,
                    CloseTime = wh.CloseTime ?? TimeSpan.Zero
                };

                _context.VenueWorkingHours.Add(workingHour);
            }
        }

        private async Task AddPlayStationDetailsAsync(Venue venue, CompletePlayStationProfileCommand request)
        {
            // Remove existing details
            var existingDetails = await _context.VenuePlayStationDetails
                .FirstOrDefaultAsync(d => d.VenueId == venue.Id);
            
            if (existingDetails != null)
            {
                _context.VenuePlayStationDetails.Remove(existingDetails);
            }

            _context.VenuePlayStationDetails.Add(new VenuePlayStationDetails
            {
                VenueId = venue.Id,
                NumberOfRooms = request.NumberOfRooms,
                HasPS4 = request.HasPS4,
                HasPS5 = request.HasPS5,
                HasVIPRooms = request.HasVIPRooms,
                HasCafe = request.HasCafe,
                HasWiFi = request.HasWiFi,
                ShowsMatches = request.ShowsMatches
            });
        }

        private async Task AddPlayStationPricingAsync(Venue venue, CompletePlayStationProfileCommand request)
        {
            // Remove existing pricing
            var existingPricing = _context.VenuePricing.Where(p => p.VenueId == venue.Id);
            foreach (var existing in existingPricing)
            {
                _context.VenuePricing.Remove(existing);
            }

            // PS4 Pricing
            if (request.HasPS4 && request.PS4Pricing != null)
            {
                await AddPlayStationModelPricingAsync(
                    venue,
                    PlayStationModel.PS4,
                    request.PS4Pricing,
                    request.HasVIPRooms);
            }

            // PS5 Pricing
            if (request.HasPS5 && request.PS5Pricing != null)
            {
                await AddPlayStationModelPricingAsync(
                    venue,
                    PlayStationModel.PS5,
                    request.PS5Pricing,
                    request.HasVIPRooms);
            }
        }

        private async Task AddPlayStationModelPricingAsync(
            Venue venue,
            PlayStationModel model,
            PlayStationPricingDto pricing,
            bool hasVIP)
        {
            // Classic room pricing
            if (pricing.ClassicRooms != null)
            {
                _context.VenuePricing.Add(new VenuePricing
                {
                    VenueId = venue.Id,
                    Type = PricingType.PlayStation,
                    PlayStationModel = model,
                    RoomType = RoomType.Classic,
                    GameMode = GameMode.Single,
                    Price = pricing.ClassicRooms.SingleModeHourPrice,
                    Description = $"{model} Classic Single Mode"
                });

                _context.VenuePricing.Add(new VenuePricing
                {
                    VenueId = venue.Id,
                    Type = PricingType.PlayStation,
                    PlayStationModel = model,
                    RoomType = RoomType.Classic,
                    GameMode = GameMode.Multiplayer,
                    Price = pricing.ClassicRooms.MultiplayerModeHourPrice,
                    Description = $"{model} Classic Multiplayer Mode"
                });
            }

            // VIP room pricing
            if (hasVIP && pricing.VIPRooms != null)
            {
                _context.VenuePricing.Add(new VenuePricing
                {
                    VenueId = venue.Id,
                    Type = PricingType.PlayStation,
                    PlayStationModel = model,
                    RoomType = RoomType.VIP,
                    GameMode = GameMode.Single,
                    Price = pricing.VIPRooms.SingleModeHourPrice,
                    Description = $"{model} VIP Single Mode"
                });

                _context.VenuePricing.Add(new VenuePricing
                {
                    VenueId = venue.Id,
                    Type = PricingType.PlayStation,
                    PlayStationModel = model,
                    RoomType = RoomType.VIP,
                    GameMode = GameMode.Multiplayer,
                    Price = pricing.VIPRooms.MultiplayerModeHourPrice,
                    Description = $"{model} VIP Multiplayer Mode"
                });
            }
        }

        private async Task AddVenueImagesAsync(Venue venue, List<string> imageUrls)
        {
            var order = 0;
            foreach (var url in imageUrls)
            {
                _context.VenueImages.Add(new VenueImage
                {
                    VenueId = venue.Id,
                    ImageUrl = url,
                    DisplayOrder = order++,
                    IsPrimary = order == 1
                });
            }
        }

        private VenueDetailsDto MapToVenueDetailsDto(Venue venue)
        {
            return new VenueDetailsDto(
                venue.Id,
                venue.Name,
                venue.VenueType.ToString(),
                (int)venue.VenueType,
                venue.IsProfileComplete,
                venue.CreatedAt,
                venue.UpdatedAt,
                venue.Latitude.HasValue && venue.Longitude.HasValue ?
                    new AddressResponseDto(
                        venue.Latitude,
                        venue.Longitude,
                        venue.StreetAddress,
                        venue.Landmark,
                        venue.District?.NameEn,
                        venue.District?.NameAr,
                        venue.District?.Governorate?.NameEn,
                        venue.District?.Governorate?.NameAr,
                        venue.FullAddress
                    ) : null
            );
        }
    }
}