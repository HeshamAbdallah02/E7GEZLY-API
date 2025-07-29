// Controllers/Venue/VenueProfileController.cs
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.VenueManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Controllers.VenueManagement
{
    /// <summary>
    /// Controller for venue profile management
    /// </summary>
    [ApiController]
    [Route("api/venue/profile")]
    [Authorize(Roles = "VenueAdmin")]
    public class VenueProfileController : ControllerBase
    {
        private readonly IVenueProfileService _venueProfileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<VenueProfileController> _logger;

        public VenueProfileController(
            IVenueProfileService venueProfileService,
            UserManager<ApplicationUser> userManager,
            ILogger<VenueProfileController> logger)
        {
            _venueProfileService = venueProfileService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Complete profile for court venues (Football/Padel)
        /// </summary>
        [HttpPost("complete/court")]
        public async Task<IActionResult> CompleteCourtProfile(
            [FromBody] CompleteCourtProfileDto dto)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                // Validate venue type
                var validType = await _venueProfileService.ValidateVenueTypeAsync(
                    userId, VenueType.FootballCourt) ||
                    await _venueProfileService.ValidateVenueTypeAsync(
                        userId, VenueType.PadelCourt);

                if (!validType)
                {
                    return BadRequest(new
                    {
                        message = "This endpoint is only for Football or Padel court venues"
                    });
                }

                var result = await _venueProfileService.CompleteCourtProfileAsync(userId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during court profile completion");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing court profile");
                return StatusCode(500, new { message = "An error occurred while completing the profile" });
            }
        }

        /// <summary>
        /// Complete profile for PlayStation venues
        /// </summary>
        [HttpPost("complete/playstation")]
        public async Task<IActionResult> CompletePlayStationProfile(
            [FromBody] CompletePlayStationProfileDto dto)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                // Validate venue type
                var validType = await _venueProfileService.ValidateVenueTypeAsync(
                    userId, VenueType.PlayStationVenue);

                if (!validType)
                {
                    return BadRequest(new
                    {
                        message = "This endpoint is only for PlayStation venues"
                    });
                }

                var result = await _venueProfileService.CompletePlayStationProfileAsync(userId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during PlayStation profile completion");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing PlayStation profile");
                return StatusCode(500, new { message = "An error occurred while completing the profile" });
            }
        }

        /// <summary>
        /// Get venue profile completion status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetProfileStatus()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var user = await _userManager.Users
                    .Include(u => u.Venue)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user?.VenueId == null)
                    return NotFound(new { message = "Venue not found" });

                var isComplete = await _venueProfileService.IsVenueProfileCompleteAsync(
                    user.VenueId.Value);

                return Ok(new
                {
                    venueId = user.VenueId,
                    isProfileComplete = isComplete,
                    venueType = user.Venue?.VenueType.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile status");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}