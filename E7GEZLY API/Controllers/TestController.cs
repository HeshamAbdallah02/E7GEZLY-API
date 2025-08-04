// Controllers/TestController.cs
using E7GEZLY_API.Attributes;
using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Cache;
using E7GEZLY_API.Services.Communication;
using E7GEZLY_API.Services.Location;
using E7GEZLY_API.Services.VenueManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        /// <summary>
        /// Test venue sub-user system components
        /// </summary>
        [HttpGet("venue-subuser-system")]
        public async Task<IActionResult> TestVenueSubUserSystem()
        {
            try
            {
                var checks = new Dictionary<string, object>();

                // Check database tables exist
                try
                {
                    // Check if VenueSubUsers table exists
                    var venueSubUsersTableCheck = await _context.Database
                        .SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VenueSubUsers'")
                        .FirstOrDefaultAsync();
                    var venueSubUsersExist = venueSubUsersTableCheck > 0;

                    // Check if VenueAuditLogs table exists
                    var auditLogsTableCheck = await _context.Database
                        .SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VenueAuditLogs'")
                        .FirstOrDefaultAsync();
                    var auditLogsExist = auditLogsTableCheck > 0;

                    // Check if RequiresSubUserSetup column exists in Venues table
                    var venueColumnCheck = await _context.Database
                        .SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Venues' AND COLUMN_NAME = 'RequiresSubUserSetup'")
                        .FirstOrDefaultAsync();
                    var venueColumnExists = venueColumnCheck > 0;

                    checks["database_tables"] = new
                    {
                        venue_sub_users = venueSubUsersExist,
                        venue_audit_logs = auditLogsExist,
                        venue_column_updated = venueColumnExists
                    };
                }
                catch (Exception ex)
                {
                    checks["database_tables"] = new { error = ex.Message };
                }

                // Check services are registered
                var subUserService = HttpContext.RequestServices.GetService<IVenueSubUserService>();
                var auditService = HttpContext.RequestServices.GetService<IVenueAuditService>();

                // Try to get cache service (might be null if caching is disabled)
                var cacheService = HttpContext.RequestServices.GetService<ICacheService>();

                checks["services_registered"] = new
                {
                    venue_sub_user_service = subUserService != null,
                    venue_audit_service = auditService != null,
                    cache_service = cacheService != null,
                    cache_note = cacheService == null ? "Caching temporarily disabled" : "Caching enabled"
                };

                // Check authorization service
                var authService = HttpContext.RequestServices.GetService<IAuthorizationService>();
                checks["authorization_service"] = authService != null;

                // Check if test venue exists (corrected - venues don't have email, users do)
                var testVenue = await _context.Venues
                    .Include(v => v.User) // Include the associated ApplicationUser
                    .Where(v => v.Name.Contains("Test") ||
                               (v.User != null && v.User.Email != null && v.User.Email.Contains("test")))
                    .FirstOrDefaultAsync();

                checks["test_data"] = new
                {
                    test_venue_exists = testVenue != null,
                    venue_id = testVenue?.Id,
                    venue_name = testVenue?.Name,
                    venue_user_email = testVenue?.User?.Email // Access email from User, not Venue
                };

                // Check venue sub-users count
                var subUsersCount = 0;
                try
                {
                    subUsersCount = await _context.VenueSubUsers.CountAsync();
                }
                catch (Exception ex)
                {
                    checks["sub_users_error"] = ex.Message;
                }

                // System status
                var allTablesExist = false;
                try
                {
                    var tableCheck = (dynamic)checks["database_tables"];
                    if (tableCheck.GetType().GetProperty("venue_sub_users") != null)
                    {
                        allTablesExist = tableCheck.venue_sub_users && tableCheck.venue_audit_logs;
                    }
                }
                catch
                {
                    allTablesExist = false;
                }

                checks["system_status"] = new
                {
                    timestamp = DateTime.UtcNow,
                    environment = _environment.EnvironmentName,
                    ready_for_testing = allTablesExist && subUserService != null && auditService != null,
                    sub_users_count = subUsersCount,
                    migration_needed = !allTablesExist
                };

                var nextSteps = new List<string>();

                if (!allTablesExist)
                {
                    nextSteps.Add("1. Run migration: dotnet ef migrations add AddVenueSubUserSystem");
                    nextSteps.Add("2. Update database: dotnet ef database update");
                }
                else
                {
                    nextSteps.Add("1. Create a test venue via registration");
                    nextSteps.Add("2. Complete venue profile");
                    nextSteps.Add("3. Test gateway login flow");
                    nextSteps.Add("4. Create first admin");
                    nextSteps.Add("5. Test sub-user login");
                    nextSteps.Add("6. Test operational endpoints");
                }

                return Ok(new
                {
                    success = true,
                    message = "Venue Sub-User System Verification",
                    checks = checks,
                    next_steps = nextSteps
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "System verification failed",
                    error = ex.Message,
                    stack_trace = _environment.IsDevelopment() ? ex.StackTrace : null
                });
            }
        }

        /// <summary>
        /// Debug endpoint to see token claims
        /// </summary>
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

                // Specific claim analysis
                TokenType = User.FindFirst("type")?.Value,
                SubUserId = User.FindFirst("subUserId")?.Value,
                SubUserRole = User.FindFirst("subUserRole")?.Value,
                Permissions = User.FindFirst("permissions")?.Value,
                VenueId = User.FindFirst("venueId")?.Value,

                // Permission parsing test
                PermissionsParsed = long.TryParse(User.FindFirst("permissions")?.Value, out var permValue)
                    ? (VenuePermissions)permValue
                    : VenuePermissions.None,

                // Required permissions test
                ViewSubUsersPermissionCheck = CheckPermission(VenuePermissions.ViewSubUsers),
                CreateSubUsersPermissionCheck = CheckPermission(VenuePermissions.CreateSubUsers),
                AdminPermissionsCheck = CheckPermission(VenuePermissions.AdminPermissions)
            };

            return Ok(analysis);
        }

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

        /// <summary>
        /// Test VenueOperational policy (should work with your token)
        /// </summary>
        [HttpGet("venue-operational-policy")]
        [Authorize(Policy = "VenueOperational")]
        public IActionResult TestVenueOperationalPolicy()
        {
            var tokenType = User.FindFirst("type")?.Value;
            var venueId = User.FindFirst("venueId")?.Value;
            var subUserId = User.FindFirst("subUserId")?.Value;
            var permissions = User.FindFirst("permissions")?.Value;

            return Ok(new
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
            });
        }

        /// <summary>
        /// Test ViewSubUsers permission with VenueOperational policy
        /// </summary>
        [HttpGet("test-view-subusers-permission")]
        [Authorize(Policy = "VenueOperational")]
        [RequireVenuePermission(VenuePermissions.ViewSubUsers)]
        public IActionResult TestViewSubUsersPermission()
        {
            var permissions = User.FindFirst("permissions")?.Value;
            var tokenType = User.FindFirst("type")?.Value;
            var venueId = User.FindFirst("venueId")?.Value;

            return Ok(new
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
            });
        }

        /// <summary>
        /// Simulate exact same setup as GetSubUsers endpoint
        /// </summary>
        [HttpGet("simulate-get-subusers/{venueId}")]
        [Authorize(Policy = "VenueOperational")]
        [RequireVenuePermission(VenuePermissions.ViewSubUsers)]
        public IActionResult SimulateGetSubUsers(Guid venueId)
        {
            // Same venue validation as real endpoint
            var tokenVenueId = User.FindFirst("venueId")?.Value;
            if (tokenVenueId != venueId.ToString())
            {
                return Forbid("Access denied: Token venue ID does not match requested venue");
            }

            return Ok(new
            {
                success = true,
                message = "SUCCESS: Simulation of GetSubUsers endpoint - all checks passed",
                simulation = new
                {
                    endpointPattern = "/api/venues/{venueId}/subusers",
                    authorizationPolicy = "VenueOperational",
                    requiredPermission = VenuePermissions.ViewSubUsers.ToString(),
                    venueIdValidation = "PASSED"
                },
                tokenInfo = new
                {
                    tokenVenueId,
                    requestedVenueId = venueId.ToString(),
                    venueIdMatch = tokenVenueId == venueId.ToString(),
                    tokenType = User.FindFirst("type")?.Value,
                    permissions = User.FindFirst("permissions")?.Value
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Test if token is valid and working (call before logout)
        /// </summary>
        [HttpGet("token-status")]
        [Authorize(Policy = "VenueOperational")]
        public IActionResult CheckTokenStatus()
        {
            var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            var subUserId = User.FindFirst("subUserId")?.Value;
            var permissions = User.FindFirst("permissions")?.Value;
            var tokenType = User.FindFirst("type")?.Value;

            return Ok(new
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
            });
        }

        /// <summary>
        /// Test admin operation (should fail after logout)
        /// </summary>
        [HttpGet("test-admin-access")]
        [Authorize(Policy = "VenueOperational")]
        [RequireVenuePermission(VenuePermissions.CreateSubUsers)]
        public IActionResult TestAdminAccess()
        {
            return Ok(new
            {
                success = true,
                message = "✅ Admin access GRANTED",
                operation = "CreateSubUsers permission test",
                details = new
                {
                    subUserId = User.FindFirst("subUserId")?.Value,
                    permissions = User.FindFirst("permissions")?.Value,
                    jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value
                },
                timestamp = DateTime.UtcNow,
                warning = "⚠️ If you see this AFTER logout, then blacklisting is NOT working!"
            });
        }

        /// <summary>
        /// Verify logout worked by trying to access this endpoint after logout
        /// </summary>
        [HttpGet("verify-logout")]
        [Authorize(Policy = "VenueOperational")]
        public IActionResult VerifyLogout()
        {
            return Ok(new
            {
                success = false,
                message = "❌ LOGOUT FAILED - Token is still valid",
                problem = "Token blacklisting is not working properly",
                tokenInfo = new
                {
                    jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value,
                    subUserId = User.FindFirst("subUserId")?.Value,
                    isAuthenticated = User.Identity?.IsAuthenticated
                },
                troubleshooting = new
                {
                    step1 = "Check if TokenBlacklistMiddleware is registered in Program.cs",
                    step2 = "Verify TokenBlacklistService is injected correctly",
                    step3 = "Check Redis/cache service is working",
                    step4 = "Look at logs for any blacklisting errors"
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Enhanced blacklist status check with cache debugging
        /// </summary>
        [HttpGet("blacklist-status/{tokenJti}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckBlacklistStatusDetailed(
            string tokenJti,
            [FromServices] ITokenBlacklistService blacklistService,
            [FromServices] ICacheService cacheService)
        {
            try
            {
                // Check blacklist status
                var isBlacklisted = await blacklistService.IsTokenBlacklistedAsync(tokenJti);

                // Try to check cache directly
                var cacheKey = $"blacklisted_token:{tokenJti}";
                object? cacheValue = null;
                var cacheError = "";

                try
                {
                    cacheValue = await cacheService.GetAsync<bool?>(cacheKey);
                }
                catch (Exception ex)
                {
                    cacheError = ex.Message;
                }

                return Ok(new
                {
                    tokenJti,
                    isBlacklisted,
                    status = isBlacklisted ? "🚫 BLACKLISTED" : "✅ NOT BLACKLISTED",
                    cacheDetails = new
                    {
                        cacheKey,
                        cacheValue,
                        cacheError = string.IsNullOrEmpty(cacheError) ? null : cacheError,
                        cacheServiceType = cacheService.GetType().Name
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    tokenJti,
                    error = "Failed to check blacklist status",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Debug endpoint to check cache service status
        /// </summary>
        [HttpGet("cache-status")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckCacheStatus([FromServices] ICacheService cacheService)
        {
            try
            {
                // Test cache with a simple operation
                var testKey = "test_cache_key";
                var testValue = "test_value";

                await cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
                var retrievedValue = await cacheService.GetAsync<string>(testKey);

                var cacheWorking = retrievedValue == testValue;

                return Ok(new
                {
                    cacheServiceType = cacheService.GetType().Name,
                    isWorking = cacheWorking,
                    testResult = cacheWorking ? "✅ CACHE WORKING" : "❌ CACHE FAILED",
                    retrievedValue,
                    expectedValue = testValue,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    cacheServiceType = cacheService?.GetType().Name ?? "NULL",
                    isWorking = false,
                    testResult = "❌ CACHE ERROR",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Manual blacklist test - blacklist a specific token
        /// </summary>
        [HttpPost("test-blacklist/{tokenJti}")]
        [AllowAnonymous]
        public async Task<IActionResult> TestBlacklistToken(
            string tokenJti,
            [FromServices] ITokenBlacklistService blacklistService)
        {
            try
            {
                await blacklistService.BlacklistTokenAsync(tokenJti, DateTime.UtcNow.AddHours(4));

                // Immediately check if it was blacklisted
                var isBlacklisted = await blacklistService.IsTokenBlacklistedAsync(tokenJti);

                return Ok(new
                {
                    success = true,
                    message = "Blacklist test completed",
                    tokenJti,
                    blacklistAttempt = "✅ SUCCESS",
                    verificationCheck = isBlacklisted ? "✅ CONFIRMED BLACKLISTED" : "❌ NOT BLACKLISTED",
                    isBlacklisted,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = "Blacklist test failed",
                    tokenJti,
                    blacklistAttempt = "❌ FAILED",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get current token's JTI for testing
        /// </summary>
        [HttpGet("my-token-jti")]
        [Authorize(Policy = "VenueOperational")]
        public IActionResult GetMyTokenJti()
        {
            var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            var subUserId = User.FindFirst("subUserId")?.Value;

            return Ok(new
            {
                jti,
                subUserId,
                message = "Use this JTI for manual blacklist testing",
                testUrl = $"/api/test/test-blacklist/{jti}",
                checkUrl = $"/api/test/blacklist-status/{jti}",
                timestamp = DateTime.UtcNow
            });
        }
    }
}