using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Account.Queries.GetCurrentUser
{
    /// <summary>
    /// Handler for GetCurrentUserQuery - returns current user profile matching AccountController logic
    /// </summary>
    public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, ApplicationResult<object>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetCurrentUserHandler> _logger;

        public GetCurrentUserHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            ILogger<GetCurrentUserHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<ApplicationResult<object>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.CustomerProfile)
                    .Include(u => u.Venue)
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (user == null)
                {
                    return ApplicationResult<object>.Failure("User not found");
                }

                // Check if it's a customer
                if (user.CustomerProfile != null)
                {
                    var profile = await _context.CustomerProfiles
                        .Include(c => c.District)
                        .ThenInclude(d => d!.Governorate)
                        .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

                    if (profile == null)
                    {
                        return ApplicationResult<object>.Failure("Customer profile not found");
                    }

                    var customerResponse = new
                    {
                        userType = "customer",
                        user = new
                        {
                            id = user.Id,
                            email = user.Email,
                            phoneNumber = user.PhoneNumber,
                            isPhoneVerified = user.IsPhoneNumberVerified,
                            isEmailVerified = user.IsEmailVerified
                        },
                        profile = new
                        {
                            id = profile.Id,
                            firstName = profile.Name.FirstName,
                            lastName = profile.Name.LastName,
                            dateOfBirth = profile.DateOfBirth,
                            address = profile.Address.FullAddress,
                            district = profile.District?.NameEn,
                            governorate = profile.District?.Governorate?.NameEn
                        }
                    };

                    return ApplicationResult<object>.Success(customerResponse);
                }

                // It's a venue
                if (user.VenueId != null)
                {
                    var venue = await _context.Venues
                        .Include(v => v.District)
                        .ThenInclude(d => d!.Governorate)
                        .FirstOrDefaultAsync(v => v.Id == user.VenueId, cancellationToken);

                    if (venue == null)
                    {
                        return ApplicationResult<object>.Failure("Venue not found");
                    }

                    var venueResponse = new
                    {
                        userType = "venue",
                        user = new
                        {
                            id = user.Id,
                            email = user.Email,
                            phoneNumber = user.PhoneNumber,
                            isPhoneVerified = user.IsPhoneNumberVerified,
                            isEmailVerified = user.IsEmailVerified
                        },
                        venue = new
                        {
                            id = venue.Id,
                            name = venue.Name.Name,
                            type = venue.VenueType.ToString(),
                            isProfileComplete = venue.IsProfileComplete,
                            location = venue.IsProfileComplete ? new
                            {
                                latitude = venue.Address.Coordinates?.Latitude,
                                longitude = venue.Address.Coordinates?.Longitude,
                                address = venue.Address.FullAddress,
                                district = venue.District?.NameEn,
                                governorate = venue.District?.Governorate?.NameEn
                            } : null
                        }
                    };

                    return ApplicationResult<object>.Success(venueResponse);
                }

                return ApplicationResult<object>.Failure("Invalid user profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user profile for user {UserId}", request.UserId);
                return ApplicationResult<object>.Failure("An error occurred while retrieving user profile");
            }
        }
    }
}