// E7GEZLY API/Controllers/VenueManagement/VenueSubUserController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using E7GEZLY_API.Extensions;
using E7GEZLY_API.Attributes;
using E7GEZLY_API.Application.Features.SubUsers.Commands.LoginSubUser;
using E7GEZLY_API.Application.Features.SubUsers.Commands.ChangeSubUserPassword;
using E7GEZLY_API.Application.Features.SubUsers.Commands.ResetSubUserPassword;
using E7GEZLY_API.Application.Features.SubUsers.Commands.LogoutSubUser;
using E7GEZLY_API.Application.Features.SubUsers.Commands.CreateSubUser;
using E7GEZLY_API.Application.Features.SubUsers.Commands.UpdateSubUser;
using E7GEZLY_API.Application.Features.SubUsers.Commands.DeleteSubUser;
using E7GEZLY_API.Application.Features.SubUsers.Queries.GetSubUsers;
using E7GEZLY_API.Application.Features.SubUsers.Queries.GetSubUser;
using E7GEZLY_API.Application.Features.SubUsers.Queries.GetAuditLogs;
using MediatR;

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
        private readonly IMediator _mediator;
        private readonly ILogger<VenueSubUserController> _logger;

        public VenueSubUserController(
            IMediator mediator,
            ILogger<VenueSubUserController> logger)
        {
            _mediator = mediator;
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

                var command = new LoginSubUserCommand
                {
                    VenueId = venueId,
                    Username = dto.Username,
                    Password = dto.Password,
                    DeviceName = HttpContext.GetDeviceName(),
                    DeviceType = HttpContext.DetectDeviceType(),
                    IpAddress = HttpContext.GetClientIpAddress(),
                    UserAgent = Request.Headers["User-Agent"].FirstOrDefault()
                };

                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return Ok(result.Data);
                }
                
                return Unauthorized(new { message = result.ErrorMessage });
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
                var query = new GetSubUsersQuery { VenueId = venueId };
                var result = await _mediator.Send(query);
                
                if (result.IsSuccess)
                {
                    return Ok(result.Data);
                }
                
                return BadRequest(new { message = result.ErrorMessage });
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
                var query = new GetSubUserQuery { VenueId = venueId, Id = subUserId };
                var result = await _mediator.Send(query);
                
                if (result.IsSuccess)
                {
                    return Ok(result.Data);
                }
                
                return NotFound(new { message = result.ErrorMessage });
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
                var command = new CreateSubUserCommand
                {
                    VenueId = venueId,
                    CreatedBySubUserId = createdBySubUserId,
                    Username = dto.Username,
                    Password = dto.Password,
                    Role = dto.Role,
                    Permissions = dto.Permissions ?? GetDefaultPermissions(dto.Role)
                };

                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return CreatedAtAction(
                        nameof(GetSubUser),
                        new { venueId, subUserId = result.Data!.Id },
                        result.Data);
                }
                
                return BadRequest(new { message = result.ErrorMessage });
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

                var command = new UpdateSubUserCommand
                {
                    VenueId = venueId,
                    Id = subUserId,
                    Role = dto.Role,
                    Permissions = dto.Permissions,
                    IsActive = dto.IsActive ?? true
                };

                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return Ok(result.Data);
                }
                
                return BadRequest(new { message = result.ErrorMessage });
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
                var command = new DeleteSubUserCommand
                {
                    VenueId = venueId,
                    Id = subUserId
                };

                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return NoContent();
                }
                
                return BadRequest(new { message = result.ErrorMessage });
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
                var command = new ChangeSubUserPasswordCommand
                {
                    VenueId = venueId,
                    SubUserId = subUserId,
                    CurrentPassword = dto.CurrentPassword,
                    NewPassword = dto.NewPassword
                };

                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Password changed successfully" });
                }
                
                return BadRequest(new { message = result.ErrorMessage });
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
                var command = new ResetSubUserPasswordCommand
                {
                    VenueId = venueId,
                    SubUserId = subUserId,
                    ResetBySubUserId = resetBySubUserId,
                    NewPassword = dto.NewPassword,
                    MustChangePassword = dto.MustChangePassword
                };

                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Password reset successfully" });
                }
                
                return BadRequest(new { message = result.ErrorMessage });
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
                var mediatorQuery = new GetAuditLogsQuery { VenueId = venueId, QueryDto = query };
                var result = await _mediator.Send(mediatorQuery);
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
                var command = new LogoutSubUserCommand { SubUserId = subUserId };
                var result = await _mediator.Send(command);
                
                if (result.IsSuccess)
                {
                    return NoContent();
                }
                
                return BadRequest(new { message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sub-user logout for venue {VenueId}", venueId);
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        private static VenuePermissions GetDefaultPermissions(VenueSubUserRole role)
        {
            return role switch
            {
                VenueSubUserRole.Admin => VenuePermissions.AdminPermissions,
                VenueSubUserRole.Operator => VenuePermissions.OperatorPermissions,
                VenueSubUserRole.Staff => VenuePermissions.StaffPermissions,
                _ => VenuePermissions.None
            };
        }
    }
}