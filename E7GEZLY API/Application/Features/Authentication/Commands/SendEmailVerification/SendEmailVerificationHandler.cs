using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SendEmailVerification
{
    /// <summary>
    /// Handler for sending email verification codes
    /// </summary>
    public class SendEmailVerificationHandler : IRequestHandler<SendEmailVerificationCommand, ApplicationResult<SendEmailVerificationResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationService _verificationService;
        private readonly ILogger<SendEmailVerificationHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public SendEmailVerificationHandler(
            UserManager<ApplicationUser> userManager,
            IVerificationService verificationService,
            ILogger<SendEmailVerificationHandler> logger,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _verificationService = verificationService;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ApplicationResult<SendEmailVerificationResponseDto>> Handle(
            SendEmailVerificationCommand request, 
            CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return ApplicationResult<SendEmailVerificationResponseDto>.Failure("User not found");
                }

                if (user.IsEmailVerified)
                {
                    return ApplicationResult<SendEmailVerificationResponseDto>.Failure("Email already verified");
                }

                if (string.IsNullOrEmpty(user.Email))
                {
                    return ApplicationResult<SendEmailVerificationResponseDto>.Failure("No email associated with this account");
                }

                // Check rate limiting
                if (user.EmailVerificationCodeExpiry.HasValue &&
                    user.EmailVerificationCodeExpiry.Value > DateTime.UtcNow.AddMinutes(8))
                {
                    var remainingMinutes = Math.Ceiling((user.EmailVerificationCodeExpiry.Value - DateTime.UtcNow).TotalMinutes - 2);
                    return ApplicationResult<SendEmailVerificationResponseDto>.Failure(
                        $"Please wait {remainingMinutes} minutes before requesting another code.");
                }

                // Generate verification code
                var (success, code) = await _verificationService.GenerateVerificationCodeAsync();
                if (!success)
                {
                    return ApplicationResult<SendEmailVerificationResponseDto>.Failure("Failed to generate verification code");
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
                    return ApplicationResult<SendEmailVerificationResponseDto>.Failure("Failed to send verification email");
                }

                _logger.LogInformation($"Email verification sent to: {user.Email}");

                var response = new SendEmailVerificationResponseDto
                {
                    Success = true,
                    Message = "Verification email sent successfully",
                    ExpiresInMinutes = 10,
                    VerificationCode = _environment.IsDevelopment() ? code : null // Only in development
                };

                return ApplicationResult<SendEmailVerificationResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification");
                return ApplicationResult<SendEmailVerificationResponseDto>.Failure("An error occurred");
            }
        }
    }
}