// Controllers/Auth/PasswordResetController.cs
using E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword;
using E7GEZLY_API.Application.Features.Authentication.Queries;
using E7GEZLY_API.Attributes;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers.Auth
{
    /// <summary>
    /// Password Reset Controller using Clean Architecture with CQRS/MediatR pattern
    /// Handles password reset operations through Application layer
    /// </summary>
    [ApiController]
    [Route("api/auth/password")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PasswordResetController> _logger;

        public PasswordResetController(
            IMediator mediator,
            ILogger<PasswordResetController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Initiate password reset process
        /// </summary>
        [HttpPost("forgot")]
        [RateLimit(3, 3600, "Password reset request rate limit exceeded. You can only request password reset 3 times per hour.")]
        public async Task<ActionResult<ApiResponse<PasswordResetResponseDto>>> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<PasswordResetResponseDto>.CreateError("Validation failed"));
                }

                var command = new ForgotPasswordCommand
                {
                    Identifier = dto.Identifier,
                    UserType = dto.UserType
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<PasswordResetResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<PasswordResetResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForgotPassword");
                return StatusCode(500, ApiResponse<PasswordResetResponseDto>.CreateError("An error occurred processing your request"));
            }
        }

        /// <summary>
        /// Validate password reset code
        /// </summary>
        [HttpPost("validate-code")]
        public async Task<ActionResult<ApiResponse<ValidateResetCodeResponseDto>>> ValidateResetCode([FromBody] ValidateResetCodeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ValidateResetCodeResponseDto>.CreateError("Validation failed"));
                }

                var command = new ValidateResetCodeCommand
                {
                    UserId = dto.UserId,
                    ResetCode = dto.ResetCode,
                    Method = dto.Method
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<ValidateResetCodeResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<ValidateResetCodeResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateResetCode");
                return StatusCode(500, ApiResponse<ValidateResetCodeResponseDto>.CreateError("An error occurred validating the reset code"));
            }
        }

        /// <summary>
        /// Reset password with validation code
        /// </summary>
        [HttpPost("reset")]
        [RateLimit(5, 3600, "Password reset rate limit exceeded. You can only reset password 5 times per hour.")]
        public async Task<ActionResult<ApiResponse<PasswordResetResponseDto>>> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<PasswordResetResponseDto>.CreateError("Validation failed"));
                }

                var command = new PerformPasswordResetCommand
                {
                    UserId = dto.UserId,
                    ResetCode = dto.ResetCode,
                    Method = dto.Method,
                    NewPassword = dto.NewPassword,
                    ConfirmPassword = dto.ConfirmPassword
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Password reset successful");
                    return Ok(ApiResponse<PasswordResetResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<PasswordResetResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPassword");
                return StatusCode(500, ApiResponse<PasswordResetResponseDto>.CreateError("An error occurred resetting your password"));
            }
        }

        /// <summary>
        /// Get available reset methods for a user
        /// </summary>
        [HttpGet("check-reset-methods/{userId}")]
        public async Task<ActionResult<ApiResponse<AvailableResetMethodsResponseDto>>> CheckAvailableResetMethods(string userId)
        {
            try
            {
                var query = new GetAvailableResetMethodsQuery
                {
                    UserId = userId
                };

                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<AvailableResetMethodsResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<AvailableResetMethodsResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAvailableResetMethods");
                return StatusCode(500, ApiResponse<AvailableResetMethodsResponseDto>.CreateError("An error occurred checking reset methods"));
            }
        }

    }
}