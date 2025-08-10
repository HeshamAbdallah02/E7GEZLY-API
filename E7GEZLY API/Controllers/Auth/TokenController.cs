// Controllers/Auth/TokenController.cs
using E7GEZLY_API.Application.Features.Authentication.Commands.RefreshToken;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers.Auth
{
    /// <summary>
    /// Token Controller using Clean Architecture with CQRS/MediatR pattern
    /// Handles token management operations through Application layer
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class TokenController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TokenController> _logger;

        public TokenController(IMediator mediator, ILogger<TokenController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        [HttpPost("token/refresh")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenDto request)
        {
            try
            {
                var ipAddress = HttpContext.GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var command = new RefreshTokenCommand
                {
                    RefreshToken = request.RefreshToken,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Token refreshed successfully");
                    return Ok(ApiResponse<AuthResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<AuthResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, ApiResponse<AuthResponseDto>.CreateError("An error occurred during token refresh"));
            }
        }

        /// <summary>
        /// Refresh authentication token (Legacy endpoint for backward compatibility)
        /// </summary>
        [HttpPost("refresh")]
        [Obsolete("Use /api/auth/token/refresh instead")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshTokenLegacy([FromBody] RefreshTokenDto request)
        {
            return await RefreshToken(request);
        }
    }
}