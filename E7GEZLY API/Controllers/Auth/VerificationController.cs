// Controllers/Auth/VerificationController.cs
using E7GEZLY_API.Application.Features.Authentication.Commands.SendVerificationCode;
using E7GEZLY_API.Application.Features.Authentication.Commands.VerifyAccount;
using E7GEZLY_API.Application.Features.Authentication.Commands.SendEmailVerification;
using E7GEZLY_API.Attributes;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers.Auth
{
    /// <summary>
    /// Verification Controller using Clean Architecture with CQRS/MediatR pattern
    /// Handles verification operations through Application layer
    /// </summary>
    [ApiController]
    [Route("api/auth/verify")]
    public class VerificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<VerificationController> _logger;

        public VerificationController(IMediator mediator, ILogger<VerificationController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Send verification code via email or SMS
        /// </summary>
        [HttpPost("send")]
        [RateLimit(10, 600, "Verification code rate limit exceeded. You can only request 10 verification codes per 10 minutes.")]
        public async Task<ActionResult<ApiResponse<SendVerificationCodeResponseDto>>> SendVerificationCode([FromBody] SendVerificationCodeDto dto)
        {
            try
            {
                var command = new SendVerificationCodeCommand
                {
                    UserId = dto.UserId,
                    Method = dto.Method,
                    Purpose = dto.Purpose
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation($"Verification code sent to user {dto.UserId} via {dto.Method}");
                    return Ok(ApiResponse<SendVerificationCodeResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<SendVerificationCodeResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification code");
                return StatusCode(500, ApiResponse<SendVerificationCodeResponseDto>.CreateError("An error occurred while sending verification code"));
            }
        }

        /// <summary>
        /// Verify user account with verification code
        /// </summary>
        [HttpPost]
        [Route("~/api/auth/verify")]  // This maintains the original route
        [RateLimit(10, 600, "Account verification rate limit exceeded. You can only attempt verification 10 times per 10 minutes.")]
        public async Task<ActionResult<ApiResponse<VerifyAccountResponseDto>>> VerifyAccount([FromBody] VerifyAccountDto dto)
        {
            try
            {
                var command = new VerifyAccountCommand
                {
                    UserId = dto.UserId,
                    VerificationCode = dto.VerificationCode,
                    Method = dto.Method
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation($"Account verified for user {dto.UserId} via {dto.Method}");
                    return Ok(ApiResponse<VerifyAccountResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<VerifyAccountResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during account verification");
                return StatusCode(500, ApiResponse<VerifyAccountResponseDto>.CreateError("An error occurred during verification"));
            }
        }

        /// <summary>
        /// Send email verification code
        /// </summary>
        [HttpPost("send-email")]
        [RateLimit(5, 600, "Email verification rate limit exceeded. You can only request email verification 5 times per 10 minutes.")]
        public async Task<ActionResult<ApiResponse<SendEmailVerificationResponseDto>>> SendEmailVerification([FromBody] SendEmailVerificationDto dto)
        {
            try
            {
                var command = new SendEmailVerificationCommand
                {
                    UserId = dto.UserId
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation($"Email verification sent to user {dto.UserId}");
                    return Ok(ApiResponse<SendEmailVerificationResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<SendEmailVerificationResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification");
                return StatusCode(500, ApiResponse<SendEmailVerificationResponseDto>.CreateError("An error occurred"));
            }
        }
    }
}