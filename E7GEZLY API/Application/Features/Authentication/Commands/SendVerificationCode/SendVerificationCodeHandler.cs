using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SendVerificationCode
{
    /// <summary>
    /// Handler for SendVerificationCodeCommand
    /// </summary>
    public class SendVerificationCodeHandler : IRequestHandler<SendVerificationCodeCommand, ApplicationResult<SendVerificationCodeResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationService _verificationService;
        private readonly AppDbContext _context;
        private readonly ILogger<SendVerificationCodeHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public SendVerificationCodeHandler(
            UserManager<ApplicationUser> userManager,
            IVerificationService verificationService,
            AppDbContext context,
            ILogger<SendVerificationCodeHandler> logger,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _verificationService = verificationService;
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ApplicationResult<SendVerificationCodeResponseDto>> Handle(SendVerificationCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return ApplicationResult<SendVerificationCodeResponseDto>.Failure("User not found");
                }

                // Check rate limiting based on purpose
                if (request.Purpose == VerificationPurpose.PasswordReset &&
                    user.LastPasswordResetRequest.HasValue &&
                    user.LastPasswordResetRequest.Value.AddMinutes(1) > DateTime.UtcNow)
                {
                    var remainingSeconds = (user.LastPasswordResetRequest.Value.AddMinutes(1) - DateTime.UtcNow).TotalSeconds;
                    return ApplicationResult<SendVerificationCodeResponseDto>.Failure(
                        $"Please wait {Math.Ceiling(remainingSeconds)} seconds before requesting another code.");
                }

                var (success, code) = await _verificationService.GenerateVerificationCodeAsync();
                if (!success)
                {
                    return ApplicationResult<SendVerificationCodeResponseDto>.Failure("Failed to generate verification code");
                }

                bool sendResult = false;

                if (request.Method == VerificationMethod.Phone)
                {
                    if (string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        return ApplicationResult<SendVerificationCodeResponseDto>.Failure("No phone number associated with this account");
                    }

                    // For password reset, check if phone is verified
                    if (request.Purpose == VerificationPurpose.PasswordReset && !user.IsPhoneNumberVerified)
                    {
                        return ApplicationResult<SendVerificationCodeResponseDto>.Failure("Phone number not verified");
                    }

                    if (request.Purpose == VerificationPurpose.AccountVerification)
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
                else if (request.Method == VerificationMethod.Email)
                {
                    if (string.IsNullOrEmpty(user.Email))
                    {
                        return ApplicationResult<SendVerificationCodeResponseDto>.Failure("No email associated with this account");
                    }

                    // For password reset, check if email is verified
                    if (request.Purpose == VerificationPurpose.PasswordReset && !user.IsEmailVerified)
                    {
                        return ApplicationResult<SendVerificationCodeResponseDto>.Failure("Email not verified");
                    }

                    if (request.Purpose == VerificationPurpose.AccountVerification)
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

                    if (request.Purpose == VerificationPurpose.AccountVerification)
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
                    _logger.LogInformation($"Verification code sent to user {user.Id} via {request.Method}");

                    var response = new SendVerificationCodeResponseDto
                    {
                        Success = true,
                        Message = $"Verification code sent to your {request.Method.ToString().ToLower()}",
                        VerificationCode = _environment.IsDevelopment() ? code : null // Only include in development
                    };

                    return ApplicationResult<SendVerificationCodeResponseDto>.Success(response);
                }

                return ApplicationResult<SendVerificationCodeResponseDto>.Failure("Failed to send verification code");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification code");
                return ApplicationResult<SendVerificationCodeResponseDto>.Failure("An error occurred while sending verification code");
            }
        }
    }
}