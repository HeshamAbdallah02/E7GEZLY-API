// Controllers/Auth/VerificationController.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Location;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace E7GEZLY_API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth/verify")]
    public class VerificationController : BaseAuthController
    {
        public VerificationController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            AppDbContext context,
            ILogger<VerificationController> logger,
            IWebHostEnvironment environment)
            : base(userManager, signInManager, tokenService, verificationService, locationService, geocodingService, context, logger, environment)
        {
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Check rate limiting based on purpose
                if (dto.Purpose == VerificationPurpose.PasswordReset &&
                    user.LastPasswordResetRequest.HasValue &&
                    user.LastPasswordResetRequest.Value.AddMinutes(1) > DateTime.UtcNow)
                {
                    var remainingSeconds = (user.LastPasswordResetRequest.Value.AddMinutes(1) - DateTime.UtcNow).TotalSeconds;
                    return BadRequest(new
                    {
                        message = $"Please wait {Math.Ceiling(remainingSeconds)} seconds before requesting another code."
                    });
                }

                var (success, code) = await _verificationService.GenerateVerificationCodeAsync();
                if (!success)
                {
                    return BadRequest(new { message = "Failed to generate verification code" });
                }

                bool sendResult = false;

                if (dto.Method == VerificationMethod.Phone)
                {
                    if (string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        return BadRequest(new { message = "No phone number associated with this account" });
                    }

                    // For password reset, check if phone is verified
                    if (dto.Purpose == VerificationPurpose.PasswordReset && !user.IsPhoneNumberVerified)
                    {
                        return BadRequest(new { message = "Phone number not verified" });
                    }

                    if (dto.Purpose == VerificationPurpose.AccountVerification) // Account verifying after registration
                    {
                        user.PhoneNumberVerificationCode = code;
                        user.PhoneNumberVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                    }
                    else // PasswordReset
                    {
                        user.PhonePasswordResetCode = code;
                        user.PhonePasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                        user.PhonePasswordResetCodeUsed = false;
                        user.LastPasswordResetRequest = DateTime.UtcNow;
                    }

                    await _userManager.UpdateAsync(user);

                    sendResult = await _verificationService.SendPhoneVerificationAsync(
                        user.PhoneNumber.Replace("+2", ""), code);
                }
                else if (dto.Method == VerificationMethod.Email)
                {
                    if (string.IsNullOrEmpty(user.Email))
                    {
                        return BadRequest(new { message = "No email associated with this account" });
                    }

                    // For password reset, check if email is verified
                    if (dto.Purpose == VerificationPurpose.PasswordReset && !user.IsEmailVerified)
                    {
                        return BadRequest(new { message = "Email not verified" });
                    }

                    if (dto.Purpose == VerificationPurpose.AccountVerification) // Account verifying after registration
                    {
                        user.EmailVerificationCode = code;
                        user.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(30);
                    }
                    else // PasswordReset
                    {
                        user.EmailPasswordResetCode = code;
                        user.EmailPasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(30);
                        user.EmailPasswordResetCodeUsed = false;
                        user.LastPasswordResetRequest = DateTime.UtcNow;
                    }

                    await _userManager.UpdateAsync(user);

                    var userName = user.CustomerProfile?.FirstName ??
                       user.Venue?.Name ??
                       user.Email!.Split('@')[0];

                    if (dto.Purpose == VerificationPurpose.AccountVerification)
                    {
                        sendResult = await _verificationService.SendEmailVerificationAsync(
                            user.Email!, userName, code);
                    }
                    else // PasswordReset
                    {
                        sendResult = await _verificationService.SendPasswordResetEmailAsync(
                            user.Email!, userName, code);
                    }
                }

                if (sendResult)
                {
                    _logger.LogInformation($"Verification code sent to user {user.Id} via {dto.Method}");

                    // Check if we're in development mode
                    if (_environment.IsDevelopment())
                    {
                        // In development, include the verification code for testing
                        return Ok(new
                        {
                            Success = true,
                            Message = $"Verification code sent to your {dto.Method.ToString().ToLower()}",
                            VerificationCode = code // This helps with testing
                        });
                    }
                    else
                    {
                        // In production, don't expose the verification code
                        return Ok(new VerificationResponseDto(
                            Success: true,
                            Message: $"Verification code sent to your {dto.Method.ToString().ToLower()}"
                        ));
                    }
                }

                return BadRequest(new VerificationResponseDto(
                    Success: false,
                    Message: "Failed to send verification code"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification code");
                return StatusCode(500, new { message = "An error occurred while sending verification code" });
            }
        }

        [HttpPost]
        [Route("~/api/auth/verify")]  // This maintains the original route
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                bool isValid = false;

                if (dto.Method == VerificationMethod.Phone)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        dto.VerificationCode,
                        user.PhoneNumberVerificationCode,
                        user.PhoneNumberVerificationCodeExpiry);

                    if (isValid)
                    {
                        user.IsPhoneNumberVerified = true;
                        user.PhoneNumberConfirmed = true;
                        user.PhoneNumberVerificationCode = null;
                        user.PhoneNumberVerificationCodeExpiry = null;
                    }
                }
                else if (dto.Method == VerificationMethod.Email)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        dto.VerificationCode,
                        user.EmailVerificationCode,
                        user.EmailVerificationCodeExpiry);

                    if (isValid)
                    {
                        user.IsEmailVerified = true;
                        user.EmailConfirmed = true;
                        user.EmailVerificationCode = null;
                        user.EmailVerificationCodeExpiry = null;
                    }
                }

                if (isValid)
                {
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation($"Account verified for user {user.Id} via {dto.Method}");

                    // Generate tokens after successful verification
                    var tokens = await _tokenService.GenerateTokensAsync(user);

                    // Check if this is a venue user
                    if (user.VenueId != null)
                    {
                        var venue = await _context.Venues.FindAsync(user.VenueId);

                        var requiredActions = new List<string>();
                        AuthMetadataDto? metadata = null;

                        if (venue != null && !venue.IsProfileComplete)
                        {
                            requiredActions.Add("COMPLETE_PROFILE");
                            metadata = new AuthMetadataDto(
                                ProfileCompletionUrl: GetProfileCompletionUrl(venue.VenueType),
                                NextStepDescription: "Complete your venue profile to start receiving bookings",
                                AdditionalData: null
                            );
                        }

                        return Ok(new
                        {
                            success = true,
                            message = "Account verified successfully",
                            tokens,
                            venueInfo = new
                            {
                                venueId = venue?.Id,
                                venueName = venue?.Name,
                                venueType = venue?.VenueType.ToString(),
                                isProfileComplete = venue?.IsProfileComplete ?? false
                            },
                            requiredActions,
                            metadata
                        });
                    }

                    // Customer verification response
                    return Ok(new
                    {
                        success = true,
                        message = "Account verified successfully",
                        tokens,
                        userType = "customer"
                    });
                }

                return BadRequest(new VerificationResponseDto(
                    Success: false,
                    Message: "Invalid or expired verification code"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during account verification");
                return StatusCode(500, new { message = "An error occurred during verification" });
            }
        }

        private string GetProfileCompletionUrl(VenueType venueType)
        {
            return venueType switch
            {
                VenueType.PlayStationVenue => "/api/venue/profile/complete/playstation",
                VenueType.FootballCourt => "/api/venue/profile/complete/court",
                VenueType.PadelCourt => "/api/venue/profile/complete/court",
                _ => "/api/venue/profile/complete"
            };
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmailVerification([FromBody] SendEmailVerificationDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return BadRequest(new { message = "User not found" });
                }

                if (user.IsEmailVerified)
                {
                    return BadRequest(new { message = "Email already verified" });
                }

                if (string.IsNullOrEmpty(user.Email))
                {
                    return BadRequest(new { message = "No email associated with this account" });
                }

                // Check rate limiting
                if (user.EmailVerificationCodeExpiry.HasValue &&
                    user.EmailVerificationCodeExpiry.Value > DateTime.UtcNow.AddMinutes(8))
                {
                    var remainingMinutes = (user.EmailVerificationCodeExpiry.Value - DateTime.UtcNow).TotalMinutes - 2;
                    return BadRequest(new
                    {
                        message = $"Please wait {Math.Ceiling(remainingMinutes)} minutes before requesting another code."
                    });
                }

                // Generate verification code
                var (success, code) = await _verificationService.GenerateVerificationCodeAsync();
                if (!success)
                {
                    return StatusCode(500, new { message = "Failed to generate verification code" });
                }

                // Save code to user
                user.EmailVerificationCode = code;
                user.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                await _userManager.UpdateAsync(user);

                // Get user name for email
                var userName = user.CustomerProfile?.FirstName ??
                              user.Venue?.Name ??
                              user.Email.Split('@')[0];

                // Send email
                var emailSent = await _verificationService.SendEmailVerificationAsync(
                    user.Email, userName, code);

                if (!emailSent)
                {
                    return StatusCode(500, new { message = "Failed to send verification email" });
                }

                _logger.LogInformation($"Email verification sent to: {user.Email}");

                var response = new
                {
                    success = true,
                    message = "Verification email sent successfully",
                    expiresInMinutes = 10
                };

                // In development, include the code
                if (_environment.IsDevelopment())
                {
                    return Ok(new
                    {
                        response.success,
                        response.message,
                        response.expiresInMinutes,
                        verificationCode = code // Development only
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}