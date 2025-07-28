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
    [Route("api/auth/social")]
    public class SocialAuthController : BaseAuthController
    {
        private readonly ISocialAuthService _socialAuthService;

        public SocialAuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            AppDbContext context,
            ILogger<SocialAuthController> logger,
            IWebHostEnvironment environment,
            ISocialAuthService socialAuthService)
            : base(userManager, signInManager, tokenService, verificationService, locationService, geocodingService, context, logger, environment)
        {
            _socialAuthService = socialAuthService;
        }

        [HttpGet("providers")]
        public IActionResult GetAvailableProviders()
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var isAppleDevice = IsAppleDevice(userAgent);
            var providers = _socialAuthService.GetAvailableProviders(isAppleDevice);

            return Ok(new AvailableProvidersDto(providers, isAppleDevice));
        }

        /// <summary>
        /// Social login for customers only. Venues must use manual registration.
        /// </summary>
        
        [HttpPost("login")]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto dto)
        {
            try
            {
                // Validate provider
                var userAgent = Request.Headers["User-Agent"].ToString();
                var isAppleDevice = IsAppleDevice(userAgent);
                var availableProviders = _socialAuthService.GetAvailableProviders(isAppleDevice);

                if (!availableProviders.Contains(dto.Provider.ToLower()))
                {
                    return BadRequest(new { message = $"Provider {dto.Provider} is not available on this device" });
                }

                // Validate token with provider
                var providerUser = await _socialAuthService.ValidateProviderTokenAsync(dto.Provider, dto.AccessToken);
                if (providerUser == null)
                {
                    return Unauthorized(new { message = "Invalid social media token" });
                }

                // Find or create user
                var user = await _socialAuthService.FindOrCreateUserAsync(dto.Provider, providerUser);
                if (user == null)
                {
                    return BadRequest(new { message = "Failed to create or find user account" });
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return Unauthorized(new { message = "Account is deactivated" });
                }

                // Update last login for external login
                await _socialAuthService.UpdateExternalLoginAsync(user, dto.Provider, providerUser.Id);

                // Create session info
                var sessionInfo = new CreateSessionDto(
                    DeviceName: dto.DeviceName ?? $"{dto.Provider} Login",
                    DeviceType: dto.DeviceType ?? "Unknown",
                    UserAgent: dto.UserAgent ?? Request.Headers["User-Agent"].ToString(),
                    IpAddress: dto.IpAddress ?? HttpContext.GetClientIpAddress()
                );

                // Generate tokens
                var authResponse = await _tokenService.GenerateTokensAsync(user, sessionInfo);

                _logger.LogInformation("Social login successful for user {UserId} via {Provider}", user.Id, dto.Provider);

                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Social login error for provider {Provider}", dto.Provider);
                return StatusCode(500, new { message = "An error occurred during social login" });
            }
        }

        /// <summary>
        /// Link social account to existing customer account
        /// </summary>

        [HttpPost("link")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> LinkSocialAccount([FromBody] SocialLoginDto dto)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Validate token
                var providerUser = await _socialAuthService.ValidateProviderTokenAsync(dto.Provider, dto.AccessToken);
                if (providerUser == null)
                {
                    return Unauthorized(new { message = "Invalid social media token" });
                }

                // Check if this social account is already linked to another user
                var existingLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(e => e.Provider == dto.Provider && e.ProviderUserId == providerUser.Id);

                if (existingLogin != null)
                {
                    if (existingLogin.UserId == userId)
                    {
                        return BadRequest(new { message = "This social account is already linked to your profile" });
                    }
                    return BadRequest(new { message = "This social account is already linked to another user" });
                }

                // Create external login
                var externalLogin = new ExternalLogin
                {
                    UserId = userId,
                    Provider = dto.Provider,
                    ProviderUserId = providerUser.Id,
                    ProviderEmail = providerUser.Email,
                    ProviderDisplayName = providerUser.Name,
                    LastLoginAt = DateTime.UtcNow
                };

                _context.ExternalLogins.Add(externalLogin);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Linked {Provider} account to user {UserId}", dto.Provider, userId);

                return Ok(new { message = $"{dto.Provider} account linked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking social account");
                return StatusCode(500, new { message = "An error occurred while linking social account" });
            }
        }

        [HttpDelete("unlink/{provider}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UnlinkSocialAccount(string provider)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Check if user has password (needed if unlinking last social account)
                var hasPassword = await _userManager.HasPasswordAsync(user);
                var socialLoginsCount = await _context.ExternalLogins
                    .CountAsync(e => e.UserId == userId);

                if (!hasPassword && socialLoginsCount <= 1)
                {
                    return BadRequest(new { message = "Cannot unlink the last social account without setting a password first" });
                }

                var externalLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider);

                if (externalLogin == null)
                {
                    return NotFound(new { message = $"{provider} account is not linked" });
                }

                _context.ExternalLogins.Remove(externalLogin);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Unlinked {Provider} account from user {UserId}", provider, userId);

                return Ok(new { message = $"{provider} account unlinked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking social account");
                return StatusCode(500, new { message = "An error occurred while unlinking social account" });
            }
        }

        [HttpGet("linked-accounts")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetLinkedAccounts()
        {
            var userId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var linkedAccounts = await _context.ExternalLogins
                .Where(e => e.UserId == userId)
                .Select(e => new
                {
                    provider = e.Provider,
                    email = e.ProviderEmail,
                    displayName = e.ProviderDisplayName,
                    linkedAt = e.CreatedAt,
                    lastLoginAt = e.LastLoginAt
                })
                .ToListAsync();

            return Ok(new { linkedAccounts });
        }

        private bool IsAppleDevice(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false;

            var appleDeviceIdentifiers = new[] { "iPhone", "iPad", "Mac", "Darwin" };
            return appleDeviceIdentifiers.Any(identifier =>
                userAgent.Contains(identifier, StringComparison.OrdinalIgnoreCase));
        }
    }
}