// Controllers/Auth/PasswordResetController.cs
using E7GEZLY_API.Attributes;
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Location;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DtoUserType = E7GEZLY_API.DTOs.Auth.UserType;
using ModelUserType = E7GEZLY_API.Models.UserType;

namespace E7GEZLY_API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth/password")]
    public class PasswordResetController : BaseAuthController
    {
        public PasswordResetController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IVerificationService verificationService,
            ILocationService locationService,
            IGeocodingService geocodingService,
            AppDbContext context,
            ILogger<PasswordResetController> logger,
            IWebHostEnvironment environment)
            : base(userManager, signInManager, tokenService, verificationService, locationService, geocodingService, context, logger, environment)
        {
        }

        [HttpPost("forgot")]
        [RateLimit(3, 3600, "Password reset request rate limit exceeded. You can only request password reset 3 times per hour.")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Log validation errors
                    var errors = ModelState
                        .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Errors = x.Value!.Errors.Select(e => e.ErrorMessage) })
                        .ToList();

                    _logger.LogWarning($"ModelState invalid: {System.Text.Json.JsonSerializer.Serialize(errors)}");
                    return BadRequest(ModelState);
                }

                ApplicationUser? user = null;

                // Check if identifier is email or phone
                if (dto.Identifier.Contains("@"))
                {
                    // It's an email
                    user = await _userManager.FindByEmailAsync(dto.Identifier);
                }
                else if (dto.Identifier.StartsWith("01") && dto.Identifier.Length == 11)
                {
                    // It's a phone number - format it
                    var formattedPhone = $"+2{dto.Identifier}";
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone);
                }
                else
                {
                    return BadRequest(new { message = "Invalid email or phone number format" });
                }

                if (user == null)
                {
                    // Don't reveal if user exists
                    return Ok(new PasswordResetResponseDto(
                        Success: true,
                        Message: "If an account exists with this information, you'll receive further instructions."
                    ));
                }

                // Verify user type matches
                bool isValidUserType = dto.UserType switch
                {
                    DtoUserType.Customer => user.VenueId == null,
                    DtoUserType.Venue => user.VenueId != null,
                    _ => false
                };

                if (!isValidUserType)
                {
                    _logger.LogWarning($"Password reset requested with wrong user type. Identifier: {dto.Identifier}, Type: {dto.UserType}");
                    return Ok(new PasswordResetResponseDto(
                        Success: true,
                        Message: "If an account exists with this information, you'll receive further instructions."
                    ));
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning($"Password reset requested for inactive account: {dto.Identifier}");
                    return Ok(new PasswordResetResponseDto(
                        Success: true,
                        Message: "If an account exists with this information, you'll receive further instructions."
                    ));
                }

                // Check if user has verified phone or email
                if (!user.IsPhoneNumberVerified && !user.IsEmailVerified)
                {
                    _logger.LogInformation($"Password reset requested for unverified account: {user.Id}");

                    // Instead of revealing the account exists, guide them to verification
                    return Ok(new PasswordResetResponseDto(
                        Success: true,
                        Message: "Your account needs to be verified first. Please check your phone/email for verification instructions.",
                        UserId: user.Id,
                        RequiresVerification: true
                    ));
                }

                // Rate limiting (only for verified accounts to avoid blocking verification)
                if (user.LastPasswordResetRequest.HasValue &&
                    user.LastPasswordResetRequest.Value.AddMinutes(1) > DateTime.UtcNow) // Should be 2 minutes on production
                {
                    var remainingSeconds = (user.LastPasswordResetRequest.Value.AddMinutes(2) - DateTime.UtcNow).TotalSeconds;
                    return BadRequest(new
                    {
                        message = $"Please wait {Math.Ceiling(remainingSeconds)} seconds before requesting another reset code."
                    });
                }

                user.LastPasswordResetRequest = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation($"Password reset initiated for user: {user.Id}");

                return Ok(new PasswordResetResponseDto(
                    Success: true,
                    Message: "Password reset process initiated. Please choose your preferred method to receive the reset code.",
                    UserId: user.Id,
                    RequiresVerification: false
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForgotPassword");
                return StatusCode(500, new { message = "An error occurred processing your request." });
            }
        }

        [HttpPost("validate-code")]
        public async Task<IActionResult> ValidateResetCode([FromBody] ValidateResetCodeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return Ok(new { isValid = false, message = "Invalid user." });
                }

                bool isValid = false;
                DateTime? expiry = null;
                bool? isUsed = null;

                if (dto.Method == ResetMethod.Phone)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        dto.ResetCode,
                        user.PhonePasswordResetCode,
                        user.PhonePasswordResetCodeExpiry);

                    expiry = user.PhonePasswordResetCodeExpiry;
                    isUsed = user.PhonePasswordResetCodeUsed;
                }
                else if (dto.Method == ResetMethod.Email)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        dto.ResetCode,
                        user.EmailPasswordResetCode,
                        user.EmailPasswordResetCodeExpiry);

                    expiry = user.EmailPasswordResetCodeExpiry;
                    isUsed = user.EmailPasswordResetCodeUsed;
                }

                if (isUsed == true)
                {
                    return Ok(new { isValid = false, message = "Reset code has already been used." });
                }

                if (!isValid)
                {
                    return Ok(new { isValid = false, message = "Invalid or expired reset code." });
                }

                var remainingMinutes = expiry.HasValue
                    ? (expiry.Value - DateTime.UtcNow).TotalMinutes
                    : 0;

                return Ok(new
                {
                    isValid = true,
                    message = "Reset code is valid.",
                    expiresInMinutes = Math.Round(remainingMinutes, 1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateResetCode");
                return StatusCode(500, new { message = "An error occurred validating the reset code." });
            }
        }

        [HttpPost("reset")]
        [RateLimit(5, 3600, "Password reset rate limit exceeded. You can only reset password 5 times per hour.")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return BadRequest(new { message = "Invalid request." });
                }

                bool isValid = false;

                if (dto.Method == ResetMethod.Phone)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        dto.ResetCode,
                        user.PhonePasswordResetCode,
                        user.PhonePasswordResetCodeExpiry);

                    if (user.PhonePasswordResetCodeUsed == true)
                    {
                        return BadRequest(new { message = "Reset code has already been used." });
                    }
                }
                else if (dto.Method == ResetMethod.Email)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        dto.ResetCode,
                        user.EmailPasswordResetCode,
                        user.EmailPasswordResetCodeExpiry);

                    if (user.EmailPasswordResetCodeUsed == true)
                    {
                        return BadRequest(new { message = "Reset code has already been used." });
                    }
                }

                if (!isValid)
                {
                    return BadRequest(new { message = "Invalid or expired reset code." });
                }

                // Reset the password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

                if (!resetResult.Succeeded)
                {
                    _logger.LogError($"Failed to reset password for user: {dto.UserId}");
                    return BadRequest(new
                    {
                        message = "Failed to reset password.",
                        errors = resetResult.Errors.Select(e => e.Description)
                    });
                }

                // Mark code as used and clear it
                if (dto.Method == ResetMethod.Phone)
                {
                    user.PhonePasswordResetCode = null;
                    user.PhonePasswordResetCodeExpiry = null;
                    user.PhonePasswordResetCodeUsed = true;
                }
                else if (dto.Method == ResetMethod.Email)
                {
                    user.EmailPasswordResetCode = null;
                    user.EmailPasswordResetCodeExpiry = null;
                    user.EmailPasswordResetCodeUsed = true;
                }

                await _userManager.UpdateAsync(user);

                _logger.LogInformation($"Password successfully reset for user: {dto.UserId}");

                return Ok(new PasswordResetResponseDto(
                    Success: true,
                    Message: "Password has been reset successfully. Please login with your new password."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPassword");
                return StatusCode(500, new { message = "An error occurred resetting your password." });
            }
        }

        [HttpGet("check-reset-methods/{userId}")]
        public async Task<IActionResult> CheckAvailableResetMethods(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var availableMethods = new List<object>();

                if (!string.IsNullOrEmpty(user.PhoneNumber) && user.IsPhoneNumberVerified)
                {
                    availableMethods.Add(new
                    {
                        method = "Phone",
                        value = ResetMethod.Phone,
                        maskedValue = MaskPhoneNumber(user.PhoneNumber)
                    });
                }

                if (!string.IsNullOrEmpty(user.Email) && user.IsEmailVerified)
                {
                    availableMethods.Add(new
                    {
                        method = "Email",
                        value = ResetMethod.Email,
                        maskedValue = MaskEmail(user.Email)
                    });
                }

                if (!availableMethods.Any())
                {
                    return BadRequest(new
                    {
                        message = "No verified contact methods available. Please contact support."
                    });
                }

                return Ok(new
                {
                    success = true,
                    availableMethods = availableMethods,
                    preferredMethod = availableMethods.First()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAvailableResetMethods");
                return StatusCode(500, new { message = "An error occurred checking reset methods" });
            }
        }

        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 6)
                return "****";

            // Remove country code if present
            var localNumber = phoneNumber.StartsWith("+2") ? phoneNumber.Substring(2) : phoneNumber;

            // Show first 3 and last 2 digits
            if (localNumber.Length >= 11)
            {
                return $"{localNumber.Substring(0, 3)}****{localNumber.Substring(localNumber.Length - 2)}";
            }

            return "****";
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return "****";

            var parts = email.Split('@');
            var localPart = parts[0];
            var domain = parts[1];

            if (localPart.Length <= 2)
            {
                return $"{localPart}****@{domain}";
            }

            return $"{localPart.Substring(0, 2)}****@{domain}";
        }
    }
}