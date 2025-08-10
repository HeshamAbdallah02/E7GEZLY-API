// Controllers/Auth/SocialAuthController.cs
using E7GEZLY_API.Application.Features.Authentication.Commands.SocialLogin;
using E7GEZLY_API.Application.Features.Authentication.Commands.LinkSocialAccount;
using E7GEZLY_API.Application.Features.Authentication.Commands.UnlinkSocialAccount;
using E7GEZLY_API.Application.Features.Authentication.Queries.GetAvailableProviders;
using E7GEZLY_API.Application.Features.Authentication.Queries.GetLinkedAccounts;
using E7GEZLY_API.Attributes;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers.Auth
{
    /// <summary>
    /// Social Authentication Controller using Clean Architecture with CQRS/MediatR pattern
    /// Handles social authentication operations through Application layer
    /// </summary>
    [ApiController]
    [Route("api/auth/social")]
    public class SocialAuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SocialAuthController> _logger;

        public SocialAuthController(IMediator mediator, ILogger<SocialAuthController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get available social authentication providers
        /// </summary>
        [HttpGet("providers")]
        public async Task<ActionResult<ApiResponse<AvailableProvidersDto>>> GetAvailableProviders()
        {
            try
            {
                var userAgent = Request.Headers["User-Agent"].ToString();
                
                var query = new GetAvailableProvidersQuery
                {
                    UserAgent = userAgent
                };

                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<AvailableProvidersDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<AvailableProvidersDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available providers");
                return StatusCode(500, ApiResponse<AvailableProvidersDto>.CreateError("An error occurred while getting available providers"));
            }
        }

        /// <summary>
        /// Social login for customers only. Venues must use manual registration.
        /// </summary>
        [HttpPost("login")]
        [RateLimit(10, 600, "Social login rate limit exceeded. You can only attempt social login 10 times per 10 minutes.")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> SocialLogin([FromBody] SocialLoginDto dto)
        {
            try
            {
                var command = new SocialLoginCommand
                {
                    Provider = dto.Provider,
                    AccessToken = dto.AccessToken,
                    DeviceName = dto.DeviceName,
                    DeviceType = dto.DeviceType,
                    UserAgent = dto.UserAgent ?? Request.Headers["User-Agent"].ToString(),
                    IpAddress = dto.IpAddress ?? HttpContext.GetClientIpAddress()
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Social login successful via {Provider}", dto.Provider);
                    return Ok(ApiResponse<AuthResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<AuthResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Social login error for provider {Provider}", dto.Provider);
                return StatusCode(500, ApiResponse<AuthResponseDto>.CreateError("An error occurred during social login"));
            }
        }

        /// <summary>
        /// Link social account to existing customer account
        /// </summary>
        [HttpPost("link")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> LinkSocialAccount([FromBody] SocialLoginDto dto)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<string>.CreateError("User not authenticated"));

                var command = new LinkSocialAccountCommand
                {
                    UserId = userId,
                    Provider = dto.Provider,
                    AccessToken = dto.AccessToken
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Linked {Provider} account to user {UserId}", dto.Provider, userId);
                    return Ok(ApiResponse<string>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<string>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking social account");
                return StatusCode(500, ApiResponse<string>.CreateError("An error occurred while linking social account"));
            }
        }

        /// <summary>
        /// Unlink social account from user account
        /// </summary>
        [HttpDelete("unlink/{provider}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> UnlinkSocialAccount(string provider)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<string>.CreateError("User not authenticated"));

                var command = new UnlinkSocialAccountCommand
                {
                    UserId = userId,
                    Provider = provider
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Unlinked {Provider} account from user {UserId}", provider, userId);
                    return Ok(ApiResponse<string>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<string>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking social account");
                return StatusCode(500, ApiResponse<string>.CreateError("An error occurred while unlinking social account"));
            }
        }

        /// <summary>
        /// Get linked social accounts for current user
        /// </summary>
        [HttpGet("linked-accounts")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<LinkedAccountsResponseDto>>> GetLinkedAccounts()
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<LinkedAccountsResponseDto>.CreateError("User not authenticated"));

                var query = new GetLinkedAccountsQuery
                {
                    UserId = userId
                };

                var result = await _mediator.Send(query);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<LinkedAccountsResponseDto>.CreateSuccess(result.Data!));
                }

                return BadRequest(ApiResponse<LinkedAccountsResponseDto>.CreateError(result.ErrorMessage!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting linked accounts");
                return StatusCode(500, ApiResponse<LinkedAccountsResponseDto>.CreateError("An error occurred while getting linked accounts"));
            }
        }
    }
}