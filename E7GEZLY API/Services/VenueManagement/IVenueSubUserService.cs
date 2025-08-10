using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.Services.VenueManagement
{
    /// <summary>
    /// Service for managing venue sub-users
    /// </summary>
    public interface IVenueSubUserService
    {
        // Authentication
        Task<VenueSubUserLoginResponseDto> AuthenticateSubUserAsync(
            Guid venueId,
            VenueSubUserLoginDto dto);

        Task<VenueSubUserResponseDto> CreateSubUserAsync(
            Guid venueId,
            Guid? createdBySubUserId,
            CreateVenueSubUserDto dto);

        Task<VenueSubUserResponseDto> UpdateSubUserAsync(
            Guid venueId,
            Guid subUserId,
            UpdateVenueSubUserDto dto);

        Task DeleteSubUserAsync(
            Guid venueId,
            Guid subUserId,
            Guid deletedBySubUserId);

        Task<VenueSubUserResponseDto> ChangePasswordAsync(
            Guid venueId,
            Guid subUserId,
            ChangeSubUserPasswordDto dto);

        Task<VenueSubUserResponseDto> ResetPasswordAsync(
            Guid venueId,
            Guid subUserId,
            Guid resetBySubUserId,
            ResetSubUserPasswordDto dto);

        Task<IEnumerable<VenueSubUserResponseDto>> GetSubUsersAsync(
            Guid venueId);

        Task<VenueSubUserResponseDto?> GetSubUserAsync(
            Guid venueId,
            Guid subUserId);

        Task<bool> ValidatePermissionsAsync(
            Guid subUserId,
            VenuePermissions requiredPermissions);

        Task RefreshTokenAsync(
            string refreshToken);

        Task LogoutAsync(
            Guid subUserId);
    }
}