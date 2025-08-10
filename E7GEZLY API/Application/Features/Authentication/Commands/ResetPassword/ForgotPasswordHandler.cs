using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DtoUserType = E7GEZLY_API.DTOs.Auth.UserType;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Handler for ForgotPasswordCommand
    /// </summary>
    public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ApplicationResult<PasswordResetResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationService _verificationService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<ForgotPasswordHandler> _logger;

        public ForgotPasswordHandler(
            UserManager<ApplicationUser> userManager,
            IVerificationService verificationService,
            IDateTimeService dateTimeService,
            ILogger<ForgotPasswordHandler> logger)
        {
            _userManager = userManager;
            _verificationService = verificationService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<PasswordResetResponseDto>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                ApplicationUser? user = null;

                // Check if identifier is email or phone
                if (request.Identifier.Contains("@"))
                {
                    // It's an email
                    user = await _userManager.FindByEmailAsync(request.Identifier);
                }
                else if (request.Identifier.StartsWith("01") && request.Identifier.Length == 11)
                {
                    // It's a phone number - format it
                    var formattedPhone = $"+2{request.Identifier}";
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == formattedPhone, cancellationToken);
                }
                else
                {
                    return ApplicationResult<PasswordResetResponseDto>.Failure("Invalid email or phone number format");
                }

                if (user == null)
                {
                    // Don't reveal if user exists
                    return ApplicationResult<PasswordResetResponseDto>.Success(new PasswordResetResponseDto(
                        Success: true,
                        Message: "If an account exists with this information, you'll receive further instructions."
                    ));
                }

                // Verify user type matches
                bool isValidUserType = request.UserType switch
                {
                    DtoUserType.Customer => user.VenueId == null,
                    DtoUserType.Venue => user.VenueId != null,
                    _ => false
                };

                if (!isValidUserType)
                {
                    _logger.LogWarning($"Password reset requested with wrong user type. Identifier: {request.Identifier}, Type: {request.UserType}");
                    return ApplicationResult<PasswordResetResponseDto>.Success(new PasswordResetResponseDto(
                        Success: true,
                        Message: "If an account exists with this information, you'll receive further instructions."
                    ));
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning($"Password reset requested for inactive account: {request.Identifier}");
                    return ApplicationResult<PasswordResetResponseDto>.Success(new PasswordResetResponseDto(
                        Success: true,
                        Message: "If an account exists with this information, you'll receive further instructions."
                    ));
                }

                // Check if user has verified phone or email
                if (!user.IsPhoneNumberVerified && !user.IsEmailVerified)
                {
                    _logger.LogInformation($"Password reset requested for unverified account: {user.Id}");

                    // Instead of revealing the account exists, guide them to verification
                    return ApplicationResult<PasswordResetResponseDto>.Success(new PasswordResetResponseDto(
                        Success: true,
                        Message: "Your account needs to be verified first. Please check your phone/email for verification instructions.",
                        UserId: user.Id,
                        RequiresVerification: true
                    ));
                }

                // Rate limiting (only for verified accounts to avoid blocking verification)
                if (user.LastPasswordResetRequest.HasValue &&
                    user.LastPasswordResetRequest.Value.AddMinutes(1) > _dateTimeService.UtcNow) // Should be 2 minutes on production
                {
                    var remainingSeconds = (user.LastPasswordResetRequest.Value.AddMinutes(2) - _dateTimeService.UtcNow).TotalSeconds;
                    return ApplicationResult<PasswordResetResponseDto>.Failure(
                        $"Please wait {Math.Ceiling(remainingSeconds)} seconds before requesting another reset code."
                    );
                }

                user.LastPasswordResetRequest = _dateTimeService.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation($"Password reset initiated for user: {user.Id}");

                return ApplicationResult<PasswordResetResponseDto>.Success(new PasswordResetResponseDto(
                    Success: true,
                    Message: "Password reset process initiated. Please choose your preferred method to receive the reset code.",
                    UserId: user.Id,
                    RequiresVerification: false
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForgotPasswordHandler");
                return ApplicationResult<PasswordResetResponseDto>.Failure("An error occurred processing your request.");
            }
        }
    }
}