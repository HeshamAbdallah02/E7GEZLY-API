// Controllers/Auth/TokenController.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Extensions;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Location;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class TokenController : BaseAuthController
    {
        public TokenController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            AppDbContext context,
            ILogger<TokenController> logger,
            IWebHostEnvironment environment)
            : base(userManager, signInManager, tokenService, verificationService, locationService, geocodingService, context, logger, environment)
        {
        }

        [HttpPost("token/refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDto dto)
        {
            try
            {
                var ipAddress = HttpContext.GetClientIpAddress();
                var tokens = await _tokenService.RefreshTokensAsync(dto.RefreshToken, ipAddress);

                if (tokens == null)
                {
                    return Unauthorized(new { message = "Invalid or expired refresh token" });
                }

                _logger.LogInformation($"Token refreshed successfully");
                return Ok(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "An error occurred during token refresh" });
            }
        }

        // Keep the old endpoint for backward compatibility but mark it as obsolete
        [HttpPost("refresh")]
        [Obsolete("Use /api/auth/token/refresh instead")]
        public async Task<IActionResult> RefreshTokenLegacy(RefreshTokenDto dto)
        {
            return await RefreshToken(dto);
        }
    }
}