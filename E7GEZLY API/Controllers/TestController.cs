using E7GEZLY_API.Application.Features.Testing.Queries.GetAnonymousTest;
using E7GEZLY_API.Application.Features.Testing.Queries.TestGeocoding;
using E7GEZLY_API.Attributes;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Cache;
using E7GEZLY_API.Services.Communication;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E7GEZLY_API.Controllers
{
    /// <summary>
    /// Test controller using Clean Architecture with MediatR
    /// </summary>
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TestController> _logger;

        public TestController(
            IMediator mediator,
            IEmailService emailService,
            IWebHostEnvironment environment,
            ILogger<TestController> logger)
        {
            _mediator = mediator;
            _emailService = emailService;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Anonymous test endpoint
        /// </summary>
        /// <returns>Public test response</returns>
        [HttpGet("anonymous")]
        public async Task<IActionResult> Anonymous()
        {
            var query = new GetAnonymousTestQuery();
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<object>.CreateSuccess(result.Data, "Anonymous test completed"));
            }

            return StatusCode(500, ApiResponse<object>.CreateError(result.ErrorMessage));
        }

        /// <summary>
        /// Customer-only test endpoint
        /// </summary>
        /// <returns>Customer access test response</returns>
        [HttpGet("customer-only")]
        [Authorize(Policy = "CustomerOnly")]
        public IActionResult CustomerOnly()
        {
            _logger.LogInformation("Customer-only test endpoint accessed");

            // Use standard claim types that ASP.NET Core maps to
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Get custom claims
            var customerId = User.FindFirst("customerId")?.Value;
            var fullName = User.FindFirst("fullName")?.Value;

            var response = new
            {
                message = "Customer access granted",
                userId,
                email,
                customerId,
                fullName
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "Customer access verified"));
        }

        /// <summary>
        /// Venue-only test endpoint
        /// </summary>
        /// <returns>Venue access test response</returns>
        [HttpGet("venue-only")]
        [Authorize(Policy = "VenueOnly")]
        public IActionResult VenueOnly()
        {
            _logger.LogInformation("Venue-only test endpoint accessed");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var venueId = User.FindFirst("venueId")?.Value;
            var venueName = User.FindFirst("venueName")?.Value;

            var response = new
            {
                message = "Venue access granted",
                userId,
                email,
                venueId,
                venueName
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "Venue access verified"));
        }

        /// <summary>
        /// Test geocoding functionality
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lng">Longitude</param>
        /// <returns>Geocoding test results</returns>
        [HttpGet("geocoding/{lat}/{lng}")]
        public async Task<IActionResult> TestGeocoding(double lat, double lng)
        {
            _logger.LogInformation("Testing geocoding for coordinates: {Latitude}, {Longitude}", lat, lng);

            var query = new TestGeocodingQuery(lat, lng);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<object>.CreateSuccess(result.Data, "Geocoding test completed"));
            }

            return StatusCode(500, ApiResponse<object>.CreateError(result.ErrorMessage));
        }

        /// <summary>
        /// Test email functionality
        /// </summary>
        /// <param name="toEmail">Email address to send test email to</param>
        /// <returns>Email test result</returns>
        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail(string toEmail)
        {
            if (_environment.IsProduction())
            {
                _logger.LogWarning("Email test endpoint accessed in production environment");
                return NotFound();
            }

            _logger.LogInformation("Testing email functionality for address: {Email}", toEmail);

            try
            {
                var sent = await _emailService.SendEmailAsync(
                    toEmail,
                    "Test Email from E7GEZLY",
                    "<h1>Hello from E7GEZLY!</h1><p>This is a test email to verify SendGrid integration.</p>",
                    "Hello from E7GEZLY! This is a test email to verify SendGrid integration."
                );

                var response = new
                {
                    success = sent,
                    message = sent ? "Email sent successfully" : "Failed to send email",
                    checkSpamFolder = true
                };

                return Ok(ApiResponse<object>.CreateSuccess(response, sent ? "Email sent" : "Email failed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email to {Email}", toEmail);

                var errorResponse = new
                {
                    message = "Error sending email",
                    error = ex.Message
                };

                return StatusCode(500, ApiResponse<object>.CreateError("Email test failed"));
            }
        }

        /// <summary>
        /// Test email configuration
        /// </summary>
        /// <returns>Email configuration status</returns>
        [HttpGet("email-config")]
        public IActionResult TestEmailConfig([FromServices] IConfiguration configuration)
        {
            if (_environment.IsProduction())
            {
                _logger.LogWarning("Email config test endpoint accessed in production environment");
                return NotFound();
            }

            var apiKey = configuration["Email:SendGrid:ApiKey"];

            var response = new
            {
                hasApiKey = !string.IsNullOrEmpty(apiKey),
                apiKeyStart = apiKey?.Substring(0, Math.Min(10, apiKey?.Length ?? 0)) + "...",
                fromEmail = configuration["Email:FromEmail"],
                fromName = configuration["Email:FromName"],
                useMockService = configuration.GetValue<bool>("Email:UseMockService")
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "Email configuration retrieved"));
        }

        /// <summary>
        /// Test general rate limiting
        /// </summary>
        /// <returns>Rate limit test response</returns>
        [HttpGet("ratelimit/general")]
        public IActionResult TestGeneralRateLimit()
        {
            var response = new
            {
                message = "Request successful",
                timestamp = DateTime.UtcNow,
                endpoint = "general",
                limit = "60 requests per minute"
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "Rate limit test completed"));
        }

        /// <summary>
        /// Test custom rate limiting
        /// </summary>
        /// <returns>Custom rate limit test response</returns>
        [HttpGet("ratelimit/custom")]
        [RateLimit(limit: 5, periodInSeconds: 60, message: "Custom endpoint limited to 5 requests per minute")]
        public IActionResult TestCustomRateLimit()
        {
            var response = new
            {
                message = "Custom limit test successful",
                timestamp = DateTime.UtcNow,
                endpoint = "custom",
                limit = "5 requests per minute"
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "Custom rate limit test completed"));
        }

        /// <summary>
        /// Test authenticated rate limiting
        /// </summary>
        /// <returns>Authenticated rate limit test response</returns>
        [HttpGet("ratelimit/authenticated")]
        [Authorize]
        public IActionResult TestAuthenticatedRateLimit()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var response = new
            {
                message = "Authenticated request successful",
                userId,
                role,
                timestamp = DateTime.UtcNow,
                endpoint = "authenticated",
                limit = role == "Customer" ? "100/minute" : "200/minute"
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "Authenticated rate limit test completed"));
        }

        /// <summary>
        /// Debug token claims
        /// </summary>
        /// <returns>Token analysis</returns>
        [HttpGet("debug-token")]
        [Authorize]
        public IActionResult DebugToken()
        {
            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            }).ToList();

            var analysis = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                AuthenticationType = User.Identity?.AuthenticationType,
                TotalClaims = claims.Count,
                Claims = claims,
                TokenType = User.FindFirst("type")?.Value,
                SubUserId = User.FindFirst("subUserId")?.Value,
                SubUserRole = User.FindFirst("subUserRole")?.Value,
                Permissions = User.FindFirst("permissions")?.Value,
                VenueId = User.FindFirst("venueId")?.Value,
                PermissionsParsed = long.TryParse(User.FindFirst("permissions")?.Value, out var permValue)
                    ? (VenuePermissions)permValue
                    : VenuePermissions.None,
                ViewSubUsersPermissionCheck = CheckPermission(VenuePermissions.ViewSubUsers),
                CreateSubUsersPermissionCheck = CheckPermission(VenuePermissions.CreateSubUsers),
                AdminPermissionsCheck = CheckPermission(VenuePermissions.AdminPermissions)
            };

            return Ok(ApiResponse<object>.CreateSuccess(analysis, "Token debug information retrieved"));
        }

        /// <summary>
        /// Test venue operational policy
        /// </summary>
        /// <returns>Policy test result</returns>
        [HttpGet("venue-operational-policy")]
        [Authorize(Policy = "VenueOperational")]
        public IActionResult TestVenueOperationalPolicy()
        {
            var tokenType = User.FindFirst("type")?.Value;
            var venueId = User.FindFirst("venueId")?.Value;
            var subUserId = User.FindFirst("subUserId")?.Value;
            var permissions = User.FindFirst("permissions")?.Value;

            var response = new
            {
                success = true,
                message = "SUCCESS: VenueOperational policy test passed",
                tokenInfo = new
                {
                    tokenType,
                    venueId,
                    subUserId,
                    permissions,
                    isOperationalToken = tokenType == "venue-operational"
                },
                timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "Venue operational policy verified"));
        }

        /// <summary>
        /// Test ViewSubUsers permission
        /// </summary>
        /// <returns>Permission test result</returns>
        [HttpGet("test-view-subusers-permission")]
        [Authorize(Policy = "VenueOperational")]
        [RequireVenuePermission(VenuePermissions.ViewSubUsers)]
        public IActionResult TestViewSubUsersPermission()
        {
            var permissions = User.FindFirst("permissions")?.Value;
            var tokenType = User.FindFirst("type")?.Value;
            var venueId = User.FindFirst("venueId")?.Value;

            var response = new
            {
                success = true,
                message = "SUCCESS: ViewSubUsers permission test passed",
                details = new
                {
                    tokenType,
                    venueId,
                    permissions,
                    requiredPermission = VenuePermissions.ViewSubUsers.ToString(),
                    requiredPermissionValue = (int)VenuePermissions.ViewSubUsers,
                    note = "Both VenueOperational policy and ViewSubUsers permission checks passed"
                },
                timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "ViewSubUsers permission verified"));
        }

        /// <summary>
        /// Test token status
        /// </summary>
        /// <returns>Token status information</returns>
        [HttpGet("token-status")]
        [Authorize(Policy = "VenueOperational")]
        public IActionResult CheckTokenStatus()
        {
            var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            var subUserId = User.FindFirst("subUserId")?.Value;
            var permissions = User.FindFirst("permissions")?.Value;
            var tokenType = User.FindFirst("type")?.Value;

            var response = new
            {
                success = true,
                message = "✅ Token is VALID and working",
                tokenInfo = new
                {
                    jti,
                    subUserId,
                    permissions,
                    tokenType,
                    isAuthenticated = User.Identity?.IsAuthenticated,
                    timestamp = DateTime.UtcNow
                },
                note = "If you can see this response, your token is valid and not blacklisted"
            };

            return Ok(ApiResponse<object>.CreateSuccess(response, "Token status verified"));
        }

        /// <summary>
        /// Check permission helper method
        /// </summary>
        private object CheckPermission(VenuePermissions required)
        {
            var permissionsClaim = User.FindFirst("permissions")?.Value;
            if (!long.TryParse(permissionsClaim, out var userPermissionsValue))
            {
                return new { HasPermission = false, Error = "Could not parse permissions" };
            }

            var userPermissions = (VenuePermissions)userPermissionsValue;
            var hasPermission = (userPermissions & required) == required;

            return new
            {
                HasPermission = hasPermission,
                UserPermissions = userPermissions.ToString(),
                UserPermissionsValue = userPermissionsValue,
                RequiredPermissions = required.ToString(),
                RequiredPermissionsValue = (long)required
            };
        }
    }
}