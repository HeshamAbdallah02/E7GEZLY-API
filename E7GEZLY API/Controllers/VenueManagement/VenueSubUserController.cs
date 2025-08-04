// E7GEZLY API/Controllers/VenueManagement/VenueSubUserController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using E7GEZLY_API.Extensions;
using E7GEZLY_API.Attributes;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.Controllers.VenueManagement
{
    /// <summary>
    /// Controller for managing venue sub-users
    /// </summary>
    [ApiController]
    [Route("api/venues/{venueId}/subusers")]
    [Authorize(Policy = "VenueOperational")]
    public class VenueSubUserController : ControllerBase
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly IVenueAuditService _auditService;
        private readonly ILogger<VenueSubUserController> _logger;

        public VenueSubUserController(
            IVenueSubUserService subUserService,
            IVenueAuditService auditService,
            ILogger<VenueSubUserController> logger)
        {
            _subUserService = subUserService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate as a sub-user
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [RequireVenueGateway]
        public async Task<IActionResult> Login(
            Guid venueId,
            [FromBody] VenueSubUserLoginDto dto)
        {
            try
            {
                // Verify venue ID from token matches route
                if (!User.IsVenue(venueId))
                {
                    return Forbid();
                }

                var result = await _subUserService.AuthenticateSubUserAsync(venueId, dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sub-user login for venue {VenueId}", venueId);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Get all sub-users for the venue
        /// </summary>
        [HttpGet]
        [RequireVenuePermission(VenuePermissions.ViewSubUsers)]
        public async Task<IActionResult> GetSubUsers(Guid venueId)
        {
            try
            {
                var subUsers = await _subUserService.GetSubUsersAsync(venueId);
                return Ok(subUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-users for venue {VenueId}", venueId);
                return StatusCode(500, new { message = "An error occurred while retrieving sub-users" });
            }
        }

        /// <summary>
        /// Get a specific sub-user
        /// </summary>
        [HttpGet("{subUserId}")]
        [RequireVenuePermission(VenuePermissions.ViewSubUsers)]
        public async Task<IActionResult> GetSubUser(Guid venueId, Guid subUserId)
        {
            try
            {
                var subUser = await _subUserService.GetSubUserAsync(venueId, subUserId);
                if (subUser == null)
                {
                    return NotFound(new { message = "Sub-user not found" });
                }
                return Ok(subUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-user {SubUserId} for venue {VenueId}", subUserId, venueId);
                return StatusCode(500, new { message = "An error occurred while retrieving the sub-user" });
            }
        }

        /// <summary>
        /// Create a new sub-user
        /// </summary>
        [HttpPost]
        [RequireVenuePermission(VenuePermissions.CreateSubUsers)]
        public async Task<IActionResult> CreateSubUser(
            Guid venueId,
            [FromBody] CreateVenueSubUserDto dto)
        {
            try
            {
                var createdBySubUserId = User.GetSubUserId();
                var result = await _subUserService.CreateSubUserAsync(
                    venueId,
                    createdBySubUserId,
                    dto);

                return CreatedAtAction(
                    nameof(GetSubUser),
                    new { venueId, subUserId = result.Id },
                    result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-user for venue {VenueId}", venueId);
                return StatusCode(500, new { message = "An error occurred while creating the sub-user" });
            }
        }

        /// <summary>
        /// Update a sub-user
        /// </summary>
        [HttpPut("{subUserId}")]
        [RequireVenuePermission(VenuePermissions.EditSubUsers)]
        public async Task<IActionResult> UpdateSubUser(
            Guid venueId,
            Guid subUserId,
            [FromBody] UpdateVenueSubUserDto dto)
        {
            try
            {
                // Prevent self-role change
                if (subUserId == User.GetSubUserId() && dto.Role.HasValue)
                {
                    return BadRequest(new { message = "Cannot change your own role" });
                }

                var result = await _subUserService.UpdateSubUserAsync(venueId, subUserId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sub-user {SubUserId} for venue {VenueId}", subUserId, venueId);
                return StatusCode(500, new { message = "An error occurred while updating the sub-user" });
            }
        }

        /// <summary>
        /// Delete a sub-user
        /// </summary>
        [HttpDelete("{subUserId}")]
        [RequireVenuePermission(VenuePermissions.DeleteSubUsers)]
        public async Task<IActionResult> DeleteSubUser(Guid venueId, Guid subUserId)
        {
            try
            {
                // Prevent self-deletion
                if (subUserId == User.GetSubUserId())
                {
                    return BadRequest(new { message = "Cannot delete your own account" });
                }

                var deletedBySubUserId = User.GetSubUserId();
                await _subUserService.DeleteSubUserAsync(venueId, subUserId, deletedBySubUserId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sub-user {SubUserId} for venue {VenueId}", subUserId, venueId);
                return StatusCode(500, new { message = "An error occurred while deleting the sub-user" });
            }
        }

        /// <summary>
        /// Change own password
        /// </summary>
        [HttpPost("me/change-password")]
        [Authorize(Policy = "VenueOperational")]
        public async Task<IActionResult> ChangeMyPassword(
            Guid venueId,
            [FromBody] ChangeSubUserPasswordDto dto)
        {
            try
            {
                var subUserId = User.GetSubUserId();
                var result = await _subUserService.ChangePasswordAsync(
                    venueId,
                    subUserId,
                    dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for sub-user in venue {VenueId}", venueId);
                return StatusCode(500, new { message = "An error occurred while changing the password" });
            }
        }

        /// <summary>
        /// Admin reset another user's password
        /// </summary>
        [HttpPost("{subUserId}/reset-password")]
        [RequireVenuePermission(VenuePermissions.ResetSubUserPasswords)]
        public async Task<IActionResult> ResetPassword(
            Guid venueId,
            Guid subUserId,
            [FromBody] ResetSubUserPasswordDto dto)
        {
            try
            {
                var resetBySubUserId = User.GetSubUserId();
                var result = await _subUserService.ResetPasswordAsync(
                    venueId,
                    subUserId,
                    resetBySubUserId,
                    dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for sub-user {SubUserId} in venue {VenueId}", subUserId, venueId);
                return StatusCode(500, new { message = "An error occurred while resetting the password" });
            }
        }

        /// <summary>
        /// Get audit logs
        /// </summary>
        [HttpGet("audit-logs")]
        [RequireVenuePermission(VenuePermissions.ViewAuditLogs)]
        public async Task<IActionResult> GetAuditLogs(
            Guid venueId,
            [FromQuery] VenueAuditLogQueryDto query)
        {
            try
            {
                var result = await _auditService.GetAuditLogsAsync(venueId, query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs for venue {VenueId}", venueId);
                return StatusCode(500, new { message = "An error occurred while retrieving audit logs" });
            }
        }

        /// <summary>
        /// Logout current sub-user
        /// </summary>
        [HttpPost("logout")]
        [Authorize(Policy = "VenueOperational")]
        public async Task<IActionResult> Logout(Guid venueId)
        {
            try
            {
                var subUserId = User.GetSubUserId();
                await _subUserService.LogoutAsync(subUserId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sub-user logout for venue {VenueId}", venueId);
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }
    }
}