// Controllers/Auth/CustomerAuthController.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.DTOs.Location;
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
    [Route("api/auth/customer")]
    public class CustomerAuthController : BaseAuthController
    {
        public CustomerAuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            AppDbContext context,
            ILogger<CustomerAuthController> logger,
            IWebHostEnvironment environment)
            : base(userManager, signInManager, tokenService, verificationService, locationService, geocodingService, context, logger, environment)
        {
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterCustomer(RegisterCustomerDto dto)
        {
            var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Age validation
                var age = DateTime.Today.Year - dto.DateOfBirth.Year;
                if (DateTime.Today.DayOfYear < dto.DateOfBirth.DayOfYear) age--;

                if (age < 15)
                {
                    return BadRequest(new { message = "You must be at least 15 years old to register." });
                }

                if (age > 80)
                {
                    return BadRequest(new { message = "Please enter a valid date of birth." });
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

                // Create user
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    PhoneNumber = formattedPhoneNumber,
                    VenueId = null,
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
                    await _userManager.AddToRoleAsync(user, DbInitializer.AppRoles.Customer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning role to user");
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "Error assigning user role", detail = ex.Message });
                }

                // Create customer profile
                var profile = new CustomerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    DateOfBirth = dto.DateOfBirth,
                    CreatedAt = DateTime.UtcNow
                };

                // Add address information if provided
                if (dto.Address != null)
                {
                    // Find the district by name
                    if (!string.IsNullOrWhiteSpace(dto.Address.Governorate) &&
                        !string.IsNullOrWhiteSpace(dto.Address.District))
                    {
                        var district = await _context.Districts
                            .Include(d => d.Governorate)
                            .FirstOrDefaultAsync(d =>
                                (d.NameEn.ToLower() == dto.Address.District.ToLower() ||
                                 d.NameAr == dto.Address.District) &&
                                (d.Governorate.NameEn.ToLower() == dto.Address.Governorate.ToLower() ||
                                 d.Governorate.NameAr == dto.Address.Governorate));

                        if (district != null)
                        {
                            profile.DistrictId = district.Id;
                        }
                    }

                    profile.StreetAddress = dto.Address.StreetAddress;
                    profile.Latitude = dto.Address.Latitude;
                    profile.Longitude = dto.Address.Longitude;
                    profile.Landmark = dto.Address.Landmark;
                }

                try
                {
                    _context.CustomerProfiles.Add(profile);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating customer profile");
                    await transaction.RollbackAsync();

                    // In development, return detailed error
                    if (_environment.IsDevelopment())
                    {
                        return StatusCode(500, new
                        {
                            message = "Error creating customer profile",
                            detail = ex.Message,
                            innerException = ex.InnerException?.Message
                        });
                    }

                    return StatusCode(500, new { message = "Error creating customer profile" });
                }

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
                    // Don't fail the registration if verification fails
                    // But log it for debugging
                }

                await transaction.CommitAsync();

                // Send welcome email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _verificationService.SendWelcomeEmailAsync(
                            user.Email,
                            profile.FirstName,
                            "Customer"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email, but registration succeeded");
                        // Don't fail - registration is already complete
                    }
                }

                _logger.LogInformation($"New customer registered: {user.Email}");

                // Check if we're in development mode
                if (_environment.IsDevelopment())
                {
                    // In development, include the verification code for testing
                    return Ok(new
                    {
                        Success = true,
                        Message = "Registration successful. Please verify your phone number.",
                        UserId = user.Id,
                        RequiresVerification = true,
                        VerificationCode = verificationCode // This helps with testing
                    });
                }
                else
                {
                    // In production, don't expose the verification code
                    return Ok(new RegistrationResponseDto(
                        Success: true,
                        Message: "Registration successful. Please verify your phone number.",
                        UserId: user.Id,
                        RequiresVerification: true
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer registration");
                await transaction.RollbackAsync();

                // In development, return detailed error
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

        [HttpPost("login")]
        public async Task<IActionResult> CustomerLogin([FromBody] LoginDto dto)
        {
            try
            {
                // Find user by email or phone
                ApplicationUser? user = await FindUserByEmailOrPhoneAsync(dto.EmailOrPhone);

                if (user == null || user.VenueId != null) // Ensure it's a customer
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

                // Check if user is verified
                if (!user.IsPhoneNumberVerified && !user.IsEmailVerified)
                {
                    return Unauthorized(new
                    {
                        message = "Account not verified",
                        userId = user.Id,
                        requiresVerification = true
                    });
                }

                _logger.LogInformation($"Customer logged in: {user.Email}");

                // Create session info using extension methods
                var sessionInfo = new CreateSessionDto(
                    DeviceName: HttpContext.GetDeviceName(),
                    DeviceType: HttpContext.DetectDeviceType(),
                    UserAgent: Request.Headers["User-Agent"].FirstOrDefault(),
                    IpAddress: HttpContext.GetClientIpAddress()
                );

                var tokens = await _tokenService.GenerateTokensAsync(user, sessionInfo);

                // Get customer profile with location
                var customerProfile = await _context.CustomerProfiles
                    .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                    .FirstOrDefaultAsync(cp => cp.UserId == user.Id);

                return Ok(new
                {
                    success = true,
                    tokens = tokens,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        phoneNumber = user.PhoneNumber,
                        isPhoneVerified = user.IsPhoneNumberVerified,
                        isEmailVerified = user.IsEmailVerified
                    },
                    profile = customerProfile != null ? new
                    {
                        id = customerProfile.Id,
                        firstName = customerProfile.FirstName,
                        lastName = customerProfile.LastName,
                        dateOfBirth = customerProfile.DateOfBirth,
                        fullAddress = customerProfile.FullAddress,
                        location = customerProfile.DistrictId.HasValue ? new
                        {
                            districtId = customerProfile.DistrictId,
                            districtName = customerProfile.District?.NameEn,
                            districtNameAr = customerProfile.District?.NameAr,
                            governorateId = customerProfile.District?.GovernorateId,
                            governorateName = customerProfile.District?.Governorate?.NameEn,
                            governorateNameAr = customerProfile.District?.Governorate?.NameAr,
                            streetAddress = customerProfile.StreetAddress,
                            landmark = customerProfile.Landmark
                        } : null
                    } : null,
                    requiredActions = GetCustomerRequiredActions(user, customerProfile),
                    metadata = GetCustomerAuthMetadata(user, customerProfile)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        // Helper method to find user by email or phone
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
                var formattedPhone = FormatEgyptianPhoneNumber(emailOrPhone);
                user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone);
            }

            return user;
        }

        // Helper method to format Egyptian phone numbers
        private string FormatEgyptianPhoneNumber(string phoneNumber)
        {
            // Remove any spaces, dashes, or special characters
            var cleaned = phoneNumber.Replace(" ", "")
                                   .Replace("-", "")
                                   .Replace("(", "")
                                   .Replace(")", "")
                                   .Replace("+", "");

            // Handle different Egyptian phone number formats
            if (cleaned.StartsWith("200")) // +200 instead of +20
            {
                cleaned = cleaned.Substring(1); // Remove the extra 0
            }

            // Add +20 prefix
            if (cleaned.StartsWith("20"))
            {
                cleaned = "+" + cleaned;
            }
            else if (cleaned.StartsWith("0"))
            {
                // Replace leading 0 with +20
                cleaned = "+20" + cleaned.Substring(1);
            }
            else if (cleaned.Length == 10 && cleaned.StartsWith("1")) // Just 10 digits starting with 1
            {
                cleaned = "+20" + cleaned;
            }
            else if (!cleaned.StartsWith("+"))
            {
                cleaned = "+20" + cleaned;
            }

            // Validate Egyptian phone number format
            // Should be +201XXXXXXXXX (13 characters total)
            if (cleaned.Length != 13 || !cleaned.StartsWith("+201"))
            {
                _logger.LogWarning($"Invalid phone number format: {phoneNumber} -> {cleaned}");
            }

            return cleaned;
        }

        // Helper method to get required actions for customers
        private List<string> GetCustomerRequiredActions(ApplicationUser user, CustomerProfile? profile)
        {
            var actions = new List<string>();

            // Check if profile exists and is complete
            if (profile == null)
            {
                actions.Add("CREATE_PROFILE");
            }
            else
            {
                // Check for incomplete profile fields
                if (string.IsNullOrWhiteSpace(profile.FirstName) ||
                    string.IsNullOrWhiteSpace(profile.LastName))
                {
                    actions.Add("COMPLETE_BASIC_INFO");
                }

                if (!profile.DateOfBirth.HasValue)
                {
                    actions.Add("ADD_BIRTH_DATE");
                }

                if (!profile.DistrictId.HasValue ||
                    string.IsNullOrWhiteSpace(profile.StreetAddress))
                {
                    actions.Add("ADD_ADDRESS");
                }
            }

            // Future: Add other checks
            // if (!user.HasCompletedOnboarding)
            //     actions.Add("COMPLETE_ONBOARDING");

            return actions;
        }

        // Helper method to get metadata for customer auth
        private AuthMetadataDto? GetCustomerAuthMetadata(ApplicationUser user, CustomerProfile? profile)
        {
            if (profile == null || GetCustomerRequiredActions(user, profile).Any())
            {
                return new AuthMetadataDto(
                    ProfileCompletionUrl: "/api/customer/profile",
                    NextStepDescription: profile == null
                        ? "Please create your profile to start booking venues"
                        : "Please complete your profile information",
                    AdditionalData: new Dictionary<string, object>
                    {
                        ["hasProfile"] = profile != null,
                        ["estimatedCompletionTime"] = "2-3 minutes"
                    }
                );
            }

            return null;
        }
    }
}