// Services/Venue/VenueProfileService.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Services.VenueManagement
{
    /// <summary>
    /// Service for managing venue profile completion
    /// </summary>
    public class VenueProfileService : IVenueProfileService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<VenueProfileService> _logger;

        public VenueProfileService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<VenueProfileService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<VenueProfileCompletionResponseDto> CompleteCourtProfileAsync(
            string userId,
            CompleteCourtProfileDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await GetUserWithVenueAsync(userId);
                if (user?.Venue == null)
                    throw new InvalidOperationException("Venue not found for user");

                var venue = user.Venue;

                // Validate venue type
                if (venue.VenueType != VenueType.FootballCourt &&
                    venue.VenueType != VenueType.PadelCourt)
                {
                    throw new InvalidOperationException(
                        $"This endpoint is for court venues only. Venue type is {venue.VenueType}");
                }

                // Update location
                await UpdateVenueLocationAsync(venue, dto);

                // Add working hours
                await AddWorkingHoursAsync(venue, dto.WorkingHours, dto);

                // Add pricing
                await AddCourtPricingAsync(venue, dto);

                // Add images if provided
                if (dto.ImageUrls?.Any() == true)
                {
                    await AddVenueImagesAsync(venue, dto.ImageUrls);
                }

                // Mark profile as complete
                venue.IsProfileComplete = true;
                venue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Court venue profile completed for venue {venue.Id}");

                return new VenueProfileCompletionResponseDto(
                    "Venue profile completed successfully",
                    true,
                    MapToVenueDetailsDto(venue)
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error completing court profile for user {userId}");
                throw;
            }
        }

        public async Task<VenueProfileCompletionResponseDto> CompletePlayStationProfileAsync(
            string userId,
            CompletePlayStationProfileDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await GetUserWithVenueAsync(userId);
                if (user?.Venue == null)
                    throw new InvalidOperationException("Venue not found for user");

                var venue = user.Venue;

                // Validate venue type
                if (venue.VenueType != VenueType.PlayStationVenue)
                {
                    throw new InvalidOperationException(
                        $"This endpoint is for PlayStation venues only. Venue type is {venue.VenueType}");
                }

                // Validate PlayStation models
                if (!dto.HasPS4 && !dto.HasPS5)
                {
                    throw new InvalidOperationException(
                        "Venue must have at least PS4 or PS5");
                }

                // Update location
                await UpdateVenueLocationAsync(venue, dto);

                // Add working hours
                await AddWorkingHoursAsync(venue, dto.WorkingHours, null);

                // Add PlayStation details
                await AddPlayStationDetailsAsync(venue, dto);

                // Add pricing
                await AddPlayStationPricingAsync(venue, dto);

                // Add images if provided
                if (dto.ImageUrls?.Any() == true)
                {
                    await AddVenueImagesAsync(venue, dto.ImageUrls);
                }

                // Mark profile as complete
                venue.IsProfileComplete = true;
                venue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"PlayStation venue profile completed for venue {venue.Id}");

                return new VenueProfileCompletionResponseDto(
                    "Venue profile completed successfully",
                    true,
                    MapToVenueDetailsDto(venue)
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error completing PlayStation profile for user {userId}");
                throw;
            }
        }

        public async Task<bool> IsVenueProfileCompleteAsync(Guid venueId)
        {
            return await _context.Venues
                .Where(v => v.Id == venueId)
                .Select(v => v.IsProfileComplete)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ValidateVenueTypeAsync(string userId, VenueType expectedType)
        {
            var user = await GetUserWithVenueAsync(userId);
            return user?.Venue?.VenueType == expectedType;
        }

        #region Private Helper Methods

        private async Task<ApplicationUser?> GetUserWithVenueAsync(string userId)
        {
            return await _userManager.Users
                .Include(u => u.Venue)
                .ThenInclude(v => v!.District)
                .ThenInclude(d => d!.Governorate)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        private async Task UpdateVenueLocationAsync(
            Models.Venue venue,
            CompleteVenueProfileBaseDto dto)
        {
            venue.Latitude = dto.Latitude;
            venue.Longitude = dto.Longitude;
            venue.DistrictId = dto.DistrictId;
            venue.StreetAddress = dto.StreetAddress;
            venue.Landmark = dto.Landmark;

            // Load district for full address computation
            if (venue.District?.Governorate == null)
            {
                venue.District = await _context.Districts
                    .Include(d => d.Governorate)
                    .FirstOrDefaultAsync(d => d.Id == dto.DistrictId);
            }
        }

        private async Task AddWorkingHoursAsync(
            Models.Venue venue,
            List<WorkingHoursDto> workingHours,
            CompleteCourtProfileDto? courtDto)
        {
            // Remove existing working hours
            _context.RemoveRange(venue.WorkingHours);

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

                // For courts, add morning/evening hours
                if (courtDto != null && !wh.IsClosed)
                {
                    workingHour.MorningStartTime = courtDto.MorningStartTime;
                    workingHour.MorningEndTime = courtDto.MorningEndTime;
                    workingHour.EveningStartTime = courtDto.EveningStartTime;
                    workingHour.EveningEndTime = courtDto.EveningEndTime;
                }

                await _context.VenueWorkingHours.AddAsync(workingHour);
            }
        }

        private async Task AddCourtPricingAsync(Models.Venue venue, CompleteCourtProfileDto dto)
        {
            // Remove existing pricing
            _context.RemoveRange(venue.Pricing);

            // Morning price
            await _context.VenuePricing.AddAsync(new VenuePricing
            {
                VenueId = venue.Id,
                Type = PricingType.MorningHour,
                Price = dto.MorningHourPrice,
                TimeSlotType = TimeSlotType.Morning,
                DepositPercentage = dto.DepositPercentage,
                Description = "Morning hour rate"
            });

            // Evening price
            await _context.VenuePricing.AddAsync(new VenuePricing
            {
                VenueId = venue.Id,
                Type = PricingType.EveningHour,
                Price = dto.EveningHourPrice,
                TimeSlotType = TimeSlotType.Evening,
                DepositPercentage = dto.DepositPercentage,
                Description = "Evening hour rate"
            });
        }

        private async Task AddPlayStationDetailsAsync(
            Models.Venue venue,
            CompletePlayStationProfileDto dto)
        {
            // Remove existing details
            if (venue.PlayStationDetails != null)
            {
                _context.Remove(venue.PlayStationDetails);
            }

            await _context.VenuePlayStationDetails.AddAsync(new VenuePlayStationDetails
            {
                VenueId = venue.Id,
                NumberOfRooms = dto.NumberOfRooms,
                HasPS4 = dto.HasPS4,
                HasPS5 = dto.HasPS5,
                HasVIPRooms = dto.HasVIPRooms,
                HasCafe = dto.HasCafe,
                HasWiFi = dto.HasWiFi,
                ShowsMatches = dto.ShowsMatches
            });
        }

        private async Task AddPlayStationPricingAsync(
            Models.Venue venue,
            CompletePlayStationProfileDto dto)
        {
            // Remove existing pricing
            _context.RemoveRange(venue.Pricing);

            // PS4 Pricing
            if (dto.HasPS4 && dto.PS4Pricing != null)
            {
                await AddPlayStationModelPricingAsync(
                    venue,
                    PlayStationModel.PS4,
                    dto.PS4Pricing,
                    dto.HasVIPRooms);
            }

            // PS5 Pricing
            if (dto.HasPS5 && dto.PS5Pricing != null)
            {
                await AddPlayStationModelPricingAsync(
                    venue,
                    PlayStationModel.PS5,
                    dto.PS5Pricing,
                    dto.HasVIPRooms);
            }
        }

        private async Task AddPlayStationModelPricingAsync(
            Models.Venue venue,
            PlayStationModel model,
            PlayStationPricingDto pricing,
            bool hasVIP)
        {
            // Classic room pricing
            if (pricing.ClassicRooms != null)
            {
                await _context.VenuePricing.AddAsync(new VenuePricing
                {
                    VenueId = venue.Id,
                    Type = PricingType.PlayStation,
                    PlayStationModel = model,
                    RoomType = RoomType.Classic,
                    GameMode = GameMode.Single,
                    Price = pricing.ClassicRooms.SingleModeHourPrice,
                    Description = $"{model} Classic Single Mode"
                });

                await _context.VenuePricing.AddAsync(new VenuePricing
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
                await _context.VenuePricing.AddAsync(new VenuePricing
                {
                    VenueId = venue.Id,
                    Type = PricingType.PlayStation,
                    PlayStationModel = model,
                    RoomType = RoomType.VIP,
                    GameMode = GameMode.Single,
                    Price = pricing.VIPRooms.SingleModeHourPrice,
                    Description = $"{model} VIP Single Mode"
                });

                await _context.VenuePricing.AddAsync(new VenuePricing
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

        private async Task AddVenueImagesAsync(Models.Venue venue, List<string> imageUrls)
        {
            var order = 0;
            foreach (var url in imageUrls)
            {
                await _context.VenueImages.AddAsync(new VenueImage
                {
                    VenueId = venue.Id,
                    ImageUrl = url,
                    DisplayOrder = order++,
                    IsPrimary = order == 1
                });
            }
        }

        private VenueDetailsDto MapToVenueDetailsDto(Models.Venue venue)
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

        #endregion
    }
}