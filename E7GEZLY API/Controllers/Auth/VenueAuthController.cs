// Controllers/Auth/VenueAuthController.cs
using E7GEZLY_API.Attributes;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Location;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Extensions;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Location;
using E7GEZLY_API.Services.VenueManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace E7GEZLY_API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth/venue")]
    public class VenueAuthController : BaseAuthController
    {
        private readonly IVenueSubUserService _subUserService;
        private readonly IConfiguration _configuration;
        public VenueAuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            IVenueSubUserService subUserService,
            IConfiguration configuration,
            AppDbContext context,
            ILogger<VenueAuthController> logger,
            IWebHostEnvironment environment)
            : base(userManager, signInManager, tokenService, verificationService, locationService, geocodingService, context, logger, environment)
        {
            _subUserService = subUserService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterVenue(RegisterVenueDto dto)
        {
            var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email already registered" });
                }

                // Check if phone number already exists
                var formattedPhoneNumber = $"+2{dto.PhoneNumber}";
                var existingPhone = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhoneNumber);
                if (existingPhone != null)
                {
                    return BadRequest(new { message = "Phone number already registered" });
                }

                // Create venue with basic info only (no location yet)
                var venue = new Venue
                {
                    Id = Guid.NewGuid(),
                    Name = dto.VenueName,
                    VenueType = dto.VenueType,
                    //Features = DetermineVenueFeatures(dto.VenueType),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Venues.Add(venue);

                // Create user
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    PhoneNumber = formattedPhoneNumber,
                    VenueId = venue.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsPhoneNumberVerified = false,
                    IsEmailVerified = false
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { errors = result.Errors });
                }

                // Assign role
                try
                {
                    await _userManager.AddToRoleAsync(user, DbInitializer.AppRoles.VenueAdmin);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning role to venue user");
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "Error assigning user role", detail = ex.Message });
                }

                // Save to generate IDs
                await _context.SaveChangesAsync();

                // Generate and send phone verification code
                string? verificationCode = null;
                try
                {
                    var (success, code) = await _verificationService.GenerateVerificationCodeAsync();
                    if (success)
                    {
                        user.PhoneNumberVerificationCode = code;
                        user.PhoneNumberVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                        await _userManager.UpdateAsync(user);

                        await _verificationService.SendPhoneVerificationAsync(dto.PhoneNumber, code);
                        verificationCode = code;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating/sending verification code");
                }

                await transaction.CommitAsync();

                // Send welcome email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _verificationService.SendWelcomeEmailAsync(
                            user.Email,
                            venue.Name,
                            "Venue"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email, but registration succeeded");
                        // Don't fail registration if email fails
                    }
                }

                _logger.LogInformation($"New venue registered: {venue.Name} ({dto.VenueType})");

                // Return response
                if (_environment.IsDevelopment())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Registration successful. Please verify your phone number.",
                        userId = user.Id,
                        venueId = venue.Id,
                        requiresVerification = true,
                        requiresProfileCompletion = true,
                        verificationCode = verificationCode // For testing only
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Registration successful. Please verify your phone number.",
                        userId = user.Id,
                        venueId = venue.Id,
                        requiresVerification = true,
                        requiresProfileCompletion = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during venue registration");
                await transaction.RollbackAsync();

                if (_environment.IsDevelopment())
                {
                    return StatusCode(500, new
                    {
                        message = "An error occurred during registration",
                        detail = ex.Message,
                        stackTrace = ex.StackTrace,
                        innerException = ex.InnerException?.Message
                    });
                }

                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }


        /// <summary>
        /// Login as venue (gateway only)
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                // Find user by email or phone
                ApplicationUser? user = await FindUserByEmailOrPhoneAsync(dto.EmailOrPhone);

                if (user == null || user.VenueId == null)
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                if (!user.IsActive)
                {
                    return Unauthorized(new { message = "Account is deactivated" });
                }

                // Check if user is verified (phone or email)
                if (!user.IsPhoneNumberVerified && !user.IsEmailVerified)
                {
                    return Unauthorized(new
                    {
                        message = "Account not verified",
                        userId = user.Id,
                        requiresVerification = true
                    });
                }

                var venue = await _context.Venues
                    .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                    .FirstOrDefaultAsync(v => v.Id == user.VenueId);

                if (venue == null)
                {
                    return Unauthorized(new { message = "Venue not found" });
                }

                _logger.LogInformation($"Venue logged in: {venue.Name}");

                // Check if sub-user setup is required
                var hasSubUsers = await _context.VenueSubUsers
                    .AnyAsync(su => su.VenueId == venue.Id);

                if (!hasSubUsers && venue.IsProfileComplete)
                {
                    venue.RequiresSubUserSetup = true;
                    await _context.SaveChangesAsync();
                }

                // Create session info for gateway token
                var sessionInfo = new CreateSessionDto(
                    DeviceName: HttpContext.GetDeviceName(),
                    DeviceType: HttpContext.DetectDeviceType(),
                    UserAgent: Request.Headers["User-Agent"].FirstOrDefault(),
                    IpAddress: HttpContext.GetClientIpAddress()
                );

                // Generate gateway token instead of regular tokens
                var gatewayToken = GenerateVenueGatewayToken(venue.Id, user.Id);

                // Determine required actions
                var requiredActions = GetRequiredActions(user, venue);
                var metadata = GetAuthMetadata(venue);

                // Build the response for gateway login
                var response = new
                {
                    // Gateway token information
                    gatewayToken = gatewayToken,
                    expiresAt = DateTime.UtcNow.AddHours(24), // Gateway tokens last longer
                    requiresSubUserSetup = venue.RequiresSubUserSetup,

                    // User information
                    user = new UserAuthInfoDto(
                        Id: user.Id,
                        Email: user.Email!,
                        PhoneNumber: user.PhoneNumber,
                        IsPhoneVerified: user.IsPhoneNumberVerified,
                        IsEmailVerified: user.IsEmailVerified
                    ),

                    // Venue information
                    venue = new VenueAuthInfoDto(
                        Id: venue.Id,
                        Name: venue.Name,
                        Type: venue.VenueType.ToString(),
                        IsProfileComplete: venue.IsProfileComplete,
                        Location: venue.IsProfileComplete ? new
                        {
                            latitude = venue.Latitude,
                            longitude = venue.Longitude,
                            streetAddress = venue.StreetAddress,
                            district = venue.District?.NameEn,
                            governorate = venue.District?.Governorate?.NameEn,
                            fullAddress = venue.FullAddress
                        } : null
                    ),

                    // Actions and metadata
                    requiredActions,
                    metadata,

                    // Instructions for next step
                    nextStep = venue.RequiresSubUserSetup ? "create-first-admin" : "sub-user-login"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during venue login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Create first admin after profile completion
        /// </summary>
        [HttpPost("create-first-admin")]
        [Authorize(Policy = "VenueGateway")]
        public async Task<IActionResult> CreateFirstAdmin([FromBody] CreateVenueSubUserDto dto)
        {
            try
            {
                var venueId = User.GetVenueId();

                // Verify no sub-users exist
                var hasSubUsers = await _context.VenueSubUsers
                    .AnyAsync(su => su.VenueId == venueId);

                if (hasSubUsers)
                {
                    return BadRequest(new { message = "Sub-users already exist" });
                }

                // Force admin role and full permissions for first admin
                var adminDto = dto with
                {
                    Role = VenueSubUserRole.Admin,
                    Permissions = VenuePermissions.AdminPermissions
                };

                var subUser = await _subUserService.CreateSubUserAsync(
                    venueId,
                    null, // No creator for first admin
                    adminDto);

                // Mark as founder and update venue
                var entity = await _context.VenueSubUsers.FindAsync(subUser.Id);
                if (entity != null)
                {
                    entity.IsFounderAdmin = true;
                    entity.MustChangePassword = false; // First admin doesn't need to change password immediately
                }

                var venue = await _context.Venues.FindAsync(venueId);
                if (venue != null)
                {
                    venue.RequiresSubUserSetup = false;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "First admin created successfully",
                    subUser = subUser,
                    nextStep = "sub-user-login"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating first admin");
                return StatusCode(500, new { message = "An error occurred while creating the first admin" });
            }
        }

        // Helper methods:
        #region Private Helper Methods
        private async Task<ApplicationUser?> FindUserByEmailOrPhoneAsync(string emailOrPhone)
        {
            ApplicationUser? user;

            // Check if input is email
            if (emailOrPhone.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(emailOrPhone);
            }
            else
            {
                // Format phone number
                var formattedPhone = FormatPhoneNumber(emailOrPhone);
                user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone);
            }

            return user;
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove any spaces or special characters
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Add +20 if not present
            if (!cleaned.StartsWith("+"))
            {
                if (cleaned.StartsWith("20"))
                {
                    cleaned = "+" + cleaned;
                }
                else if (cleaned.StartsWith("0"))
                {
                    cleaned = "+20" + cleaned.Substring(1);
                }
                else
                {
                    cleaned = "+20" + cleaned;
                }
            }

            return cleaned;
        }

        private List<string> GetRequiredActions(ApplicationUser user, Models.Venue venue)
        {
            var actions = new List<string>();

            if (!venue.IsProfileComplete)
                actions.Add("COMPLETE_PROFILE");

            // Future actions can be added here
            // if (!venue.IsVerified)
            //     actions.Add("AWAIT_ADMIN_VERIFICATION");

            // if (!venue.HasActiveSubscription)
            //     actions.Add("CHOOSE_SUBSCRIPTION_PLAN");

            return actions;
        }

        private AuthMetadataDto? GetAuthMetadata(Models.Venue venue)
        {
            if (!venue.IsProfileComplete)
            {
                return new AuthMetadataDto(
                    ProfileCompletionUrl: venue.VenueType switch
                    {
                        VenueType.PlayStationVenue => "/api/venue/profile/complete/playstation",
                        VenueType.FootballCourt => "/api/venue/profile/complete/court",
                        VenueType.PadelCourt => "/api/venue/profile/complete/court",
                        _ => "/api/venue/profile/complete"
                    },
                    NextStepDescription: "Complete your venue profile to start receiving bookings",
                    AdditionalData: new Dictionary<string, object>
                    {
                        ["venueType"] = venue.VenueType.ToString(),
                        ["estimatedCompletionTime"] = "5-10 minutes"
                    }
                );
            }

            return null;
        }

        private string GenerateVenueGatewayToken(Guid venueId, string userId)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("venueId", venueId.ToString()),
        new Claim("type", "venue-gateway")
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(24); // Gateway tokens last longer

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion
    }
}