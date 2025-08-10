using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Handler for RequestPasswordResetCommand
    /// </summary>
    public class RequestPasswordResetHandler : IRequestHandler<RequestPasswordResetCommand, ApplicationResult<PasswordResetRequestResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationService _verificationService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<RequestPasswordResetHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public RequestPasswordResetHandler(
            UserManager<ApplicationUser> userManager,
            IVerificationService verificationService,
            IDateTimeService dateTimeService,
            ILogger<RequestPasswordResetHandler> logger,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _verificationService = verificationService;
            _dateTimeService = dateTimeService;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ApplicationResult<PasswordResetRequestResponseDto>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user by email or phone
                ApplicationUser? user = null;
                string maskedContact = string.Empty;

                if (request.EmailOrPhone.Contains("@"))
                {
                    user = await _userManager.FindByEmailAsync(request.EmailOrPhone);
                    maskedContact = MaskEmail(request.EmailOrPhone);
                }
                else
                {
                    var formattedPhone = request.EmailOrPhone.StartsWith("+2") ? 
                        request.EmailOrPhone : $"+2{request.EmailOrPhone}";
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone, cancellationToken);
                    maskedContact = MaskPhone(formattedPhone);
                }

                if (user == null)
                {
                    // Don't reveal if user exists - return success with generic message
                    return ApplicationResult<PasswordResetRequestResponseDto>.Success(new PasswordResetRequestResponseDto
                    {
                        Success = true,
                        Message = "If an account with this information exists, you will receive a reset code.",
                        ResetMethod = request.ResetMethod,
                        MaskedContact = maskedContact
                    });
                }

                if (!user.IsActive)
                {
                    return ApplicationResult<PasswordResetRequestResponseDto>.Failure("Account is inactive");
                }

                // Generate reset code
                var (success, code) = await _verificationService.GenerateVerificationCodeAsync();
                if (!success)
                {
                    return ApplicationResult<PasswordResetRequestResponseDto>.Failure("Unable to generate reset code");
                }

                // Set reset code and expiry
                user.PasswordResetCode = code;
                user.PasswordResetCodeExpiry = _dateTimeService.UtcNow.AddMinutes(10);
                await _userManager.UpdateAsync(user);

                // Send reset code
                if (request.ResetMethod.Equals("Email", StringComparison.OrdinalIgnoreCase) && 
                    !string.IsNullOrEmpty(user.Email))
                {
                    await _verificationService.SendPasswordResetEmailAsync(user.Email, user.UserName ?? user.Email, code);
                }
                else if (request.ResetMethod.Equals("Phone", StringComparison.OrdinalIgnoreCase) && 
                         !string.IsNullOrEmpty(user.PhoneNumber))
                {
                    var phoneToSend = user.PhoneNumber.Replace("+2", "");
                    await _verificationService.SendPhoneVerificationAsync(phoneToSend, code);
                }
                else
                {
                    return ApplicationResult<PasswordResetRequestResponseDto>.Failure("Invalid reset method");
                }

                _logger.LogInformation($"Password reset code sent to user {user.Id} via {request.ResetMethod}");

                var response = new PasswordResetRequestResponseDto
                {
                    Success = true,
                    Message = $"Reset code sent via {request.ResetMethod}. Please check your {request.ResetMethod.ToLower()}.",
                    ResetMethod = request.ResetMethod,
                    MaskedContact = maskedContact,
                    VerificationCode = _environment.IsDevelopment() ? code : null
                };

                return ApplicationResult<PasswordResetRequestResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request");
                return ApplicationResult<PasswordResetRequestResponseDto>.Failure("An error occurred while processing the request");
            }
        }

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return email;

            var username = parts[0];
            var domain = parts[1];

            if (username.Length <= 2)
                return $"{username}@{domain}";

            return $"{username[0]}***{username[^1]}@{domain}";
        }

        private static string MaskPhone(string phone)
        {
            if (phone.Length < 6)
                return phone;

            return $"{phone[..3]}****{phone[^2..]}";
        }
    }
}