// Controllers/Auth/AccountController.cs
using E7GEZLY_API.Controllers.Auth;
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Extensions;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth/account")]
    [Authorize]
    public class AccountController : BaseAuthController
    {
        private readonly IProfileService _profileService;
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            AppDbContext context,
            ILogger<AccountController> logger,
            IWebHostEnvironment environment,
            IProfileService profileService)
            : base(userManager, signInManager, tokenService, verificationService, locationService, geocodingService, context, logger, environment)
        {
            _profileService = profileService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users
                .Include(u => u.CustomerProfile)
                .Include(u => u.Venue)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Check if it's a customer
            if (user.CustomerProfile != null)
            {
                var profile = await _context.CustomerProfiles
                    .Include(c => c.District)
                    .ThenInclude(d => d!.Governorate)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                return Ok(new
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
                        id = profile!.Id,
                        firstName = profile.FirstName,
                        lastName = profile.LastName,
                        dateOfBirth = profile.DateOfBirth,
                        address = profile.FullAddress,
                        district = profile.District?.NameEn,
                        governorate = profile.District?.Governorate?.NameEn
                    }
                });
            }

            // It's a venue
            if (user.VenueId != null)
            {
                var venue = await _context.Venues
                    .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                    .FirstOrDefaultAsync(v => v.Id == user.VenueId);

                return Ok(new
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
                        id = venue!.Id,
                        name = venue.Name,
                        type = venue.VenueType.ToString(),
                        isProfileComplete = venue.IsProfileComplete,
                        location = venue.IsProfileComplete ? new
                        {
                            latitude = venue.Latitude,
                            longitude = venue.Longitude,
                            address = venue.FullAddress,
                            district = venue.District?.NameEn,
                            governorate = venue.District?.Governorate?.NameEn
                        } : null
                    }
                });
            }

            return BadRequest(new { message = "Invalid user profile" });
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var userId = HttpContext.GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated" });

            var currentToken = HttpContext.GetCurrentRefreshToken();
            var sessions = await _tokenService.GetActiveSessionsAsync(userId, currentToken);

            return Ok(new SessionsResponseDto(sessions, sessions.Count()));
        }

        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId)
        {
            var userId = HttpContext.GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated" });

            var success = await _tokenService.RevokeSessionAsync(userId, sessionId);
            if (!success)
                return NotFound(new { message = "Session not found" });

            return Ok(new { message = "Session revoked successfully" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = HttpContext.GetCurrentRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest(new { message = "No active session found" });

            var success = await _tokenService.RevokeTokenAsync(refreshToken);
            if (!success)
                return BadRequest(new { message = "Failed to logout" });

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("logout-all-devices")]
        public async Task<IActionResult> LogoutAllDevices()
        {
            var userId = HttpContext.GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated" });

            var success = await _tokenService.RevokeAllUserTokensAsync(userId);
            if (!success)
                return BadRequest(new { message = "No active sessions found" });

            return Ok(new { message = "Logged out from all devices successfully" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = HttpContext.GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors });
            }

            // Optionally logout all devices if requested
            if (dto.LogoutAllDevices)
            {
                await _tokenService.RevokeAllUserTokensAsync(userId);
            }

            _logger.LogInformation($"Password changed for user: {user.Email}");
            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("deactivate")]
        public async Task<IActionResult> DeactivateAccount(DeactivateAccountDto dto)
        {
            var userId = HttpContext.GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated" });

            // Use ProfileService which now handles everything including session revocation
            var success = await _profileService.DeactivateAccountAsync(userId, dto.Password, dto.Reason);
            if (!success)
                return BadRequest(new { message = "Failed to deactivate account. Please check your password." });

            return Ok(new { message = "Account deactivated successfully" });
        }

        [HttpGet("check-auth")]
        public IActionResult CheckAuth()
        {
            var userId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Not authenticated" });

            return Ok(new { authenticated = true, userId });
        }
    }
}