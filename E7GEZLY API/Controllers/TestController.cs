// Controllers/TestController.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Communication;
using E7GEZLY_API.Services.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using E7GEZLY_API.Attributes;

namespace E7GEZLY_API.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly IGeocodingService _geocodingService;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;

        public TestController(IGeocodingService geocodingService, AppDbContext context, IEmailService emailService, IWebHostEnvironment environment)
        {
            _geocodingService = geocodingService;
            _context = context;
            _emailService = emailService;
            _environment = environment;
        }

        [HttpGet("anonymous")]
        public IActionResult Anonymous()
        {
            return Ok(new { message = "This endpoint is public" });
        }

        [HttpGet("customer-only")]
        [Authorize(Policy = "CustomerOnly")]
        public IActionResult CustomerOnly()
        {
            // Use standard claim types that ASP.NET Core maps to
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Get custom claims
            var customerId = User.FindFirst("customerId")?.Value;
            var fullName = User.FindFirst("fullName")?.Value;

            return Ok(new
            {
                message = "Customer access granted",
                userId,
                email,
                customerId,
                fullName
            });
        }

        [HttpGet("venue-only")]
        [Authorize(Policy = "VenueOnly")]
        public IActionResult VenueOnly()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var venueId = User.FindFirst("venueId")?.Value;
            var venueName = User.FindFirst("venueName")?.Value;

            return Ok(new
            {
                message = "Venue access granted",
                userId,
                email,
                venueId,
                venueName
            });
        }

        [HttpGet("geocoding/{lat}/{lng}")]
        public async Task<IActionResult> TestGeocoding(double lat, double lng)
        {
            try
            {
                // Test 1: Get full address info
                var addressInfo = await _geocodingService.GetAddressFromCoordinatesAsync(lat, lng);

                // Test 2: Get district ID
                var districtId = await _geocodingService.GetDistrictIdFromCoordinatesAsync(lat, lng);

                // Get district details if found
                District? district = null;
                if (districtId.HasValue)
                {
                    district = await _context.Districts
                        .Include(d => d.Governorate)
                        .FirstOrDefaultAsync(d => d.Id == districtId.Value);
                }

                return Ok(new
                {
                    coordinates = new { latitude = lat, longitude = lng },
                    addressInfo = addressInfo,
                    districtId = districtId,
                    districtDetails = district != null ? new
                    {
                        id = district.Id,
                        nameEn = district.NameEn,
                        nameAr = district.NameAr,
                        governorate = district.Governorate.NameEn
                    } : null,
                    success = districtId.HasValue
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    error = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }

        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail(string toEmail)
        {
            if (_environment.IsProduction())
                return NotFound();

            try
            {
                var sent = await _emailService.SendEmailAsync(
                    toEmail,
                    "Test Email from E7GEZLY",
                    "<h1>Hello from E7GEZLY!</h1><p>This is a test email to verify SendGrid integration.</p>",
                    "Hello from E7GEZLY! This is a test email to verify SendGrid integration."
                );

                return Ok(new
                {
                    success = sent,
                    message = sent ? "Email sent successfully" : "Failed to send email",
                    checkSpamFolder = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error sending email",
                    error = ex.Message
                });
            }
        }

        // In TestController.cs, add:
        [HttpGet("email-config")]
        public IActionResult TestEmailConfig([FromServices] IConfiguration configuration)
        {
            if (_environment.IsProduction())
                return NotFound();

            var apiKey = configuration["Email:SendGrid:ApiKey"];

            return Ok(new
            {
                hasApiKey = !string.IsNullOrEmpty(apiKey),
                apiKeyStart = apiKey?.Substring(0, Math.Min(10, apiKey?.Length ?? 0)) + "...",
                fromEmail = configuration["Email:FromEmail"],
                fromName = configuration["Email:FromName"],
                useMockService = configuration.GetValue<bool>("Email:UseMockService")
            });
        }

        [HttpGet("ratelimit/general")]
        public IActionResult TestGeneralRateLimit()
        {
            return Ok(new
            {
                message = "Request successful",
                timestamp = DateTime.UtcNow,
                endpoint = "general",
                limit = "60 requests per minute"
            });
        }

        [HttpGet("ratelimit/custom")]
        [RateLimit(limit: 5, periodInSeconds: 60, message: "Custom endpoint limited to 5 requests per minute")]
        public IActionResult TestCustomRateLimit()
        {
            return Ok(new
            {
                message = "Custom limit test successful",
                timestamp = DateTime.UtcNow,
                endpoint = "custom",
                limit = "5 requests per minute"
            });
        }

        [HttpGet("ratelimit/authenticated")]
        [Authorize]
        public IActionResult TestAuthenticatedRateLimit()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                message = "Authenticated request successful",
                userId,
                role,
                timestamp = DateTime.UtcNow,
                endpoint = "authenticated",
                limit = role == "Customer" ? "100/minute" : "200/minute"
            });
        }
    }
}