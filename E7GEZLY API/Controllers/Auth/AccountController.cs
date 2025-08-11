using E7GEZLY_API.Application.Features.Account.Commands.ChangePassword;
using E7GEZLY_API.Application.Features.Account.Commands.DeactivateAccount;
using E7GEZLY_API.Application.Features.Account.Commands.Logout;
using E7GEZLY_API.Application.Features.Account.Commands.LogoutAllDevices;
using E7GEZLY_API.Application.Features.Account.Commands.RevokeSession;
using E7GEZLY_API.Application.Features.Account.Queries.CheckAuthStatus;
using E7GEZLY_API.Application.Features.Account.Queries.GetActiveSessions;
using E7GEZLY_API.Application.Features.Account.Queries.GetCurrentUser;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers.Auth
{
    /// <summary>
    /// Account Management Controller using Clean Architecture with CQRS/MediatR pattern
    /// Handles user account operations through Application layer
    /// </summary>
    [ApiController]
    [Route("api/auth/account")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IMediator mediator, ILogger<AccountController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get current authenticated user profile
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser()
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.CreateError("User not authenticated"));
                }

                var query = new GetCurrentUserQuery { UserId = userId };
                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(result.Data!));
                }

                return NotFound(ApiResponse<object>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, ApiResponse<object>.CreateError("An error occurred while retrieving user profile"));
            }
        }

        /// <summary>
        /// Get active sessions for the current user
        /// </summary>
        [HttpGet("sessions")]
        public async Task<ActionResult<ApiResponse<SessionsResponseDto>>> GetActiveSessions()
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<SessionsResponseDto>.CreateError("User not authenticated"));
                }

                var currentToken = HttpContext.GetCurrentRefreshToken();
                var query = new GetActiveSessionsQuery 
                { 
                    UserId = userId, 
                    CurrentRefreshToken = currentToken 
                };
                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<SessionsResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<SessionsResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions");
                return StatusCode(500, ApiResponse<SessionsResponseDto>.CreateError("An error occurred while retrieving sessions"));
            }
        }

        /// <summary>
        /// Revoke a specific session
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<ActionResult<ApiResponse<object>>> RevokeSession(Guid sessionId)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.CreateError("User not authenticated"));
                }

                var command = new RevokeSessionCommand 
                { 
                    UserId = userId, 
                    SessionId = sessionId 
                };
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(result.Data!));
                }

                return NotFound(ApiResponse<object>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
                return StatusCode(500, ApiResponse<object>.CreateError("An error occurred while revoking session"));
            }
        }

        /// <summary>
        /// Logout from current session
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<object>>> Logout()
        {
            try
            {
                var refreshToken = HttpContext.GetCurrentRefreshToken();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest(ApiResponse<object>.CreateError("No active session found"));
                }

                var command = new LogoutCommand { RefreshToken = refreshToken };
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<object>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, ApiResponse<object>.CreateError("An error occurred during logout"));
            }
        }

        /// <summary>
        /// Logout from all devices
        /// </summary>
        [HttpPost("logout-all-devices")]
        public async Task<ActionResult<ApiResponse<object>>> LogoutAllDevices()
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.CreateError("User not authenticated"));
                }

                var command = new LogoutAllDevicesCommand { UserId = userId };
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<object>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout all devices");
                return StatusCode(500, ApiResponse<object>.CreateError("An error occurred during logout"));
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.CreateError("Validation failed"));
                }

                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.CreateError("User not authenticated"));
                }

                var command = new ChangePasswordCommand
                {
                    UserId = userId,
                    CurrentPassword = dto.CurrentPassword,
                    NewPassword = dto.NewPassword,
                    LogoutAllDevices = dto.LogoutAllDevices
                };
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<object>.CreateError(result.ErrorMessage!, result.Errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, ApiResponse<object>.CreateError("An error occurred while changing password"));
            }
        }

        /// <summary>
        /// Deactivate user account
        /// </summary>
        [HttpPost("deactivate")]
        public async Task<ActionResult<ApiResponse<object>>> DeactivateAccount(DeactivateAccountDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.CreateError("Validation failed"));
                }

                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.CreateError("User not authenticated"));
                }

                var command = new DeactivateAccountCommand
                {
                    UserId = userId,
                    Password = dto.Password,
                    Reason = dto.Reason
                };
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<object>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating account");
                return StatusCode(500, ApiResponse<object>.CreateError("An error occurred while deactivating account"));
            }
        }

        /// <summary>
        /// Check authentication status
        /// </summary>
        [HttpGet("check-auth")]
        public async Task<ActionResult<ApiResponse<object>>> CheckAuth()
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.CreateError("Not authenticated"));
                }

                var query = new CheckAuthStatusQuery { UserId = userId };
                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(result.Data!));
                }

                return Unauthorized(ApiResponse<object>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking auth status");
                return StatusCode(500, ApiResponse<object>.CreateError("An error occurred while checking authentication"));
            }
        }
    }
}