using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Customer;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.User;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Services.Auth
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ILogger<ProfileService> logger)
        {
            _context = context;
            _userManager = userManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<CustomerProfileResponseDto?> GetCustomerProfileAsync(string userId)
        {
            var profile = await _context.CustomerProfiles
                .Include(c => c.District)
                .ThenInclude(d => d!.Governorate)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (profile == null)
            {
                _logger.LogWarning($"Customer profile not found for user: {userId}");
                return null;
            }

            return new CustomerProfileResponseDto(
                "customer",
                MapToUserInfo(profile.User),
                MapToCustomerDetails(profile)
            );
        }

        public async Task<VenueProfileResponseDto?> GetVenueProfileAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Venue)
                .ThenInclude(v => v!.District)
                .ThenInclude(d => d!.Governorate)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.VenueId == null || user.Venue == null)
            {
                _logger.LogWarning($"Venue profile not found for user: {userId}");
                return null;
            }

            return new VenueProfileResponseDto(
                "venue",
                MapToUserInfo(user),
                MapToVenueDetails(user.Venue)
            );
        }

        public async Task<bool> DeactivateAccountAsync(string userId, string password, string? reason)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for deactivation: {userId}");
                    return false;
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, password);
                if (!passwordCheck)
                {
                    _logger.LogWarning($"Invalid password for account deactivation: {userId}");
                    return false;
                }

                // Deactivate the user account
                user.IsActive = false;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError($"Failed to update user during deactivation: {userId}. Errors: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                    return false;
                }

                // Update security stamp to invalidate all existing tokens
                await _userManager.UpdateSecurityStampAsync(user);

                // Revoke all active sessions
                await _tokenService.RevokeAllUserTokensAsync(userId);

                _logger.LogInformation($"User {userId} deactivated their account. Reason: {reason ?? "No reason provided"}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating account for user: {userId}");
                return false;
            }
        }

        #region Mapping Methods

        private static UserInfoDto MapToUserInfo(ApplicationUser user)
        {
            return new UserInfoDto(
                user.Id,
                user.Email ?? string.Empty,
                user.PhoneNumber ?? string.Empty,
                user.IsPhoneNumberVerified,
                user.IsEmailVerified,
                user.IsActive,
                user.CreatedAt
            );
        }

        private static CustomerDetailsDto MapToCustomerDetails(CustomerProfile profile)
        {
            return new CustomerDetailsDto(
                profile.Id,
                profile.FirstName,
                profile.LastName,
                profile.FullName,
                profile.DateOfBirth,
                MapCustomerAddressToResponse(profile)
            );
        }

        private static VenueDetailsDto MapToVenueDetails(Venue venue)
        {
            return new VenueDetailsDto(
                venue.Id,
                venue.Name,
                venue.VenueType.ToString(),
                (int)venue.VenueType,
                venue.Features.ToString(),
                (int)venue.Features,
                venue.IsProfileComplete,
                venue.CreatedAt,
                venue.UpdatedAt,
                venue.IsProfileComplete ? MapVenueAddressToResponse(venue) : null
            );
        }

        private static AddressResponseDto? MapCustomerAddressToResponse(CustomerProfile profile)
        {
            // Return null if no meaningful address data exists
            if (profile.District == null &&
                string.IsNullOrWhiteSpace(profile.StreetAddress) &&
                !profile.Latitude.HasValue &&
                !profile.Longitude.HasValue)
            {
                return null;
            }

            return new AddressResponseDto(
                profile.Latitude,
                profile.Longitude,
                profile.StreetAddress,
                profile.Landmark,
                profile.District?.NameEn,
                profile.District?.NameAr,
                profile.District?.Governorate?.NameEn,
                profile.District?.Governorate?.NameAr,
                profile.FullAddress
            );
        }

        private static AddressResponseDto? MapVenueAddressToResponse(Venue venue)
        {
            // Return null if no location data
            if (!venue.Latitude.HasValue && !venue.Longitude.HasValue && venue.District == null)
            {
                return null;
            }

            return new AddressResponseDto(
                venue.Latitude,
                venue.Longitude,
                venue.StreetAddress,
                venue.Landmark,
                venue.District?.NameEn,
                venue.District?.NameAr,
                venue.District?.Governorate?.NameEn,
                venue.District?.Governorate?.NameAr,
                venue.FullAddress
            );
        }

        #endregion

        #region Helper Methods

        private async Task<ApplicationUser?> GetUserWithNavigationPropertiesAsync(string userId)
        {
            return await _userManager.Users
                .Include(u => u.CustomerProfile)
                .ThenInclude(c => c!.District)
                .ThenInclude(d => d!.Governorate)
                .Include(u => u.Venue)
                .ThenInclude(v => v!.District)
                .ThenInclude(d => d!.Governorate)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        #endregion
    }

    // Extension methods for cleaner code (optional)
    public static class ProfileServiceExtensions
    {
        public static bool HasCompleteAddress(this CustomerProfile profile)
        {
            return profile.District != null || !string.IsNullOrWhiteSpace(profile.StreetAddress);
        }

        public static bool HasCompleteLocation(this Venue venue)
        {
            return venue.Latitude.HasValue && venue.Longitude.HasValue && venue.District != null;
        }
    }
}