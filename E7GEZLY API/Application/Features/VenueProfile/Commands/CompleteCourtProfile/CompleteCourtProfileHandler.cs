using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.Services;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;
using DomainEntities = E7GEZLY_API.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.VenueProfile.Commands.CompleteCourtProfile
{
    /// <summary>
    /// Handler for CompleteCourtProfileCommand
    /// </summary>
    public class CompleteCourtProfileHandler : IRequestHandler<CompleteCourtProfileCommand, ApplicationResult<VenueProfileCompletionResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly IVenueProfileCompletionService _profileCompletionService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<CompleteCourtProfileHandler> _logger;

        public CompleteCourtProfileHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            IVenueProfileCompletionService profileCompletionService,
            IDateTimeService dateTimeService,
            ILogger<CompleteCourtProfileHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _profileCompletionService = profileCompletionService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<VenueProfileCompletionResponseDto>> Handle(CompleteCourtProfileCommand request, CancellationToken cancellationToken)
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

                // Validate venue type using domain service - convert Models enum to Domain enum
                var domainVenueType = (Domain.Enums.VenueType)venue.VenueType;
                var isValidVenueType = _profileCompletionService.CanCompleteCourtProfile(domainVenueType);
                if (!isValidVenueType)
                {
                    return ApplicationResult<VenueProfileCompletionResponseDto>.Failure(
                        $"This endpoint is for court venues only. Venue type is {venue.VenueType}");
                }

                // Update location
                await UpdateVenueLocationAsync(venue, request);

                // Add working hours
                await AddWorkingHoursAsync(venue, request.WorkingHours, request);

                // Add pricing
                await AddCourtPricingAsync(venue, request);

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

                _logger.LogInformation($"Court venue profile completed for venue {venue.Id}");

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
                throw; // Re-throw to let validation handle it
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, $"Error completing court profile for user {request.UserId}");
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

        private async Task UpdateVenueLocationAsync(Venue venue, CompleteCourtProfileCommand request)
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

        private async Task AddWorkingHoursAsync(Venue venue, List<WorkingHoursDto> workingHours, CompleteCourtProfileCommand request)
        {
            // Remove existing working hours
            var existingHours = _context.VenueWorkingHours.Where(wh => wh.VenueId == venue.Id);
            foreach (var existing in existingHours)
            {
                _context.VenueWorkingHours.Remove(existing);
            }

            foreach (var wh in workingHours)
            {
                var workingHour = Domain.Entities.VenueWorkingHours.Create(
                    venue.Id,
                    wh.DayOfWeek,
                    wh.OpenTime ?? TimeSpan.Zero,
                    wh.CloseTime ?? TimeSpan.Zero,
                    wh.IsClosed,
                    !wh.IsClosed ? request.MorningStartTime : null,
                    !wh.IsClosed ? request.MorningEndTime : null,
                    !wh.IsClosed ? request.EveningStartTime : null,
                    !wh.IsClosed ? request.EveningEndTime : null
                );

                _context.VenueWorkingHours.Add(workingHour);
            }
        }

        private async Task AddCourtPricingAsync(Venue venue, CompleteCourtProfileCommand request)
        {
            // Remove existing pricing
            var existingPricing = _context.VenuePricing.Where(p => p.VenueId == venue.Id);
            foreach (var existing in existingPricing)
            {
                _context.VenuePricing.Remove(existing);
            }

            // Morning price - Create Domain Entity
            var morningPricing = Domain.Entities.VenuePricing.Create(
                venue.Id,
                PricingType.MorningHour,
                request.MorningHourPrice,
                "Morning hour rate",
                psModel: null,
                roomType: null,
                gameMode: null,
                timeSlotType: TimeSlotType.Morning,
                depositPercentage: request.DepositPercentage
            );
            _context.VenuePricing.Add(morningPricing);

            // Evening price - Create Domain Entity
            var eveningPricing = Domain.Entities.VenuePricing.Create(
                venue.Id,
                PricingType.EveningHour,
                request.EveningHourPrice,
                "Evening hour rate",
                psModel: null,
                roomType: null,
                gameMode: null,
                timeSlotType: TimeSlotType.Evening,
                depositPercentage: request.DepositPercentage
            );
            _context.VenuePricing.Add(eveningPricing);
        }

        private async Task AddVenueImagesAsync(Venue venue, List<string> imageUrls)
        {
            var order = 0;
            foreach (var url in imageUrls)
            {
                var venueImage = Domain.Entities.VenueImage.Create(
                    venue.Id,
                    url,
                    caption: null,
                    displayOrder: order++,
                    isPrimary: order == 1
                );
                _context.VenueImages.Add(venueImage);
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