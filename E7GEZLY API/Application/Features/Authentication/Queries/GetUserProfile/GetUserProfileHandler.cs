using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.User;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetUserProfile
{
    /// <summary>
    /// Handler for GetUserProfileQuery
    /// </summary>
    public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, ApplicationResult<UserProfileDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetUserProfileHandler> _logger;

        public GetUserProfileHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            ILogger<GetUserProfileHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<ApplicationResult<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Venue)
                    .ThenInclude(v => v.WorkingHours)
                    .Include(u => u.Venue)
                    .ThenInclude(v => v.Pricing)
                    .Include(u => u.Venue)
                    .ThenInclude(v => v.Images)
                    .Include(u => u.Venue)
                    .ThenInclude(v => v.PlayStationDetails)
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (user == null)
                {
                    return ApplicationResult<UserProfileDto>.Failure("User not found");
                }

                var roles = await _userManager.GetRolesAsync(user);

                var userProfile = new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber!,
                    IsEmailVerified = user.IsEmailVerified,
                    IsPhoneNumberVerified = user.IsPhoneNumberVerified,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt ?? DateTime.MinValue,
                    Roles = roles.ToList()
                };

                // Include venue information if user is venue admin
                if (user.Venue != null)
                {
                    userProfile.Venue = new VenueProfileDto
                    {
                        Id = user.Venue.Id,
                        Name = user.Venue.Name,
                        VenueType = user.Venue.VenueType.ToString(),
                        Description = user.Venue.Description,
                        Address = user.Venue.StreetAddress,
                        City = user.Venue.City,
                        Governorate = user.Venue.Governorate,
                        Latitude = user.Venue.Latitude,
                        Longitude = user.Venue.Longitude,
                        PhoneNumber = user.Venue.PhoneNumber,
                        WhatsAppNumber = user.Venue.WhatsAppNumber,
                        FacebookUrl = user.Venue.FacebookUrl,
                        InstagramUrl = user.Venue.InstagramUrl,
                        IsProfileComplete = user.Venue.IsProfileComplete,
                        IsActive = user.Venue.IsActive,
                        CreatedAt = user.Venue.CreatedAt,
                        UpdatedAt = user.Venue.UpdatedAt,
                        WorkingHours = user.Venue.WorkingHours?.Select(wh => new VenueWorkingHoursDto
                        {
                            Id = wh.Id,
                            DayOfWeek = wh.DayOfWeek,
                            OpenTime = wh.OpenTime,
                            CloseTime = wh.CloseTime,
                            IsActive = wh.IsActive
                        }).ToList() ?? new List<VenueWorkingHoursDto>(),
                        Pricings = user.Venue.Pricing?.Select(p => new VenuePricingDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            PricePerHour = p.PricePerHour,
                            Description = p.Description,
                            IsActive = p.IsActive
                        }).ToList() ?? new List<VenuePricingDto>(),
                        Images = user.Venue.Images?.Where(img => img.IsActive).Select(img => new VenueImageDto
                        {
                            Id = img.Id,
                            ImageUrl = img.ImageUrl,
                            ImageType = img.ImageType.ToString(),
                            DisplayOrder = img.DisplayOrder,
                            IsMainImage = img.IsMainImage
                        }).ToList() ?? new List<VenueImageDto>()
                    };

                    // Include PlayStation details if venue type is PlayStation
                    if (user.Venue.PlayStationDetails != null)
                    {
                        userProfile.Venue.PlayStationDetails = new VenuePlayStationDetailsDto(
                            NumberOfRooms: user.Venue.PlayStationDetails.NumberOfRooms,
                            HasPS4: user.Venue.PlayStationDetails.HasPS4,
                            HasPS5: user.Venue.PlayStationDetails.HasPS5,
                            HasVIPRooms: user.Venue.PlayStationDetails.HasVIPRooms,
                            HasCafe: user.Venue.PlayStationDetails.HasCafe,
                            HasWiFi: user.Venue.PlayStationDetails.HasWiFi,
                            ShowsMatches: user.Venue.PlayStationDetails.ShowsMatches
                        );
                    }
                }

                return ApplicationResult<UserProfileDto>.Success(userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user profile for user {request.UserId}");
                return ApplicationResult<UserProfileDto>.Failure("An error occurred while retrieving user profile");
            }
        }
    }
}