using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.ResetPassword
{
    /// <summary>
    /// Handler for PerformPasswordResetCommand
    /// </summary>
    public class PerformPasswordResetHandler : IRequestHandler<PerformPasswordResetCommand, ApplicationResult<PasswordResetResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationService _verificationService;
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<PerformPasswordResetHandler> _logger;

        public PerformPasswordResetHandler(
            UserManager<ApplicationUser> userManager,
            IVerificationService verificationService,
            IApplicationDbContext context,
            IDateTimeService dateTimeService,
            ILogger<PerformPasswordResetHandler> logger)
        {
            _userManager = userManager;
            _verificationService = verificationService;
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<PasswordResetResponseDto>> Handle(PerformPasswordResetCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return ApplicationResult<PasswordResetResponseDto>.Failure("Invalid request.");
                }

                bool isValid = false;

                if (request.Method == ResetMethod.Phone)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        request.ResetCode,
                        user.PhonePasswordResetCode,
                        user.PhonePasswordResetCodeExpiry);

                    if (user.PhonePasswordResetCodeUsed == true)
                    {
                        return ApplicationResult<PasswordResetResponseDto>.Failure("Reset code has already been used.");
                    }
                }
                else if (request.Method == ResetMethod.Email)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        request.ResetCode,
                        user.EmailPasswordResetCode,
                        user.EmailPasswordResetCodeExpiry);

                    if (user.EmailPasswordResetCodeUsed == true)
                    {
                        return ApplicationResult<PasswordResetResponseDto>.Failure("Reset code has already been used.");
                    }
                }

                if (!isValid)
                {
                    return ApplicationResult<PasswordResetResponseDto>.Failure("Invalid or expired reset code.");
                }

                // Reset the password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

                if (!resetResult.Succeeded)
                {
                    _logger.LogError($"Failed to reset password for user: {request.UserId}");
                    return ApplicationResult<PasswordResetResponseDto>.Failure(
                        resetResult.Errors.Select(e => e.Description).ToArray());
                }

                // Mark code as used and clear it
                if (request.Method == ResetMethod.Phone)
                {
                    user.PhonePasswordResetCode = null;
                    user.PhonePasswordResetCodeExpiry = null;
                    user.PhonePasswordResetCodeUsed = true;
                }
                else if (request.Method == ResetMethod.Email)
                {
                    user.EmailPasswordResetCode = null;
                    user.EmailPasswordResetCodeExpiry = null;
                    user.EmailPasswordResetCodeUsed = true;
                }

                await _userManager.UpdateAsync(user);

                // Invalidate user sessions (optional - force re-login)
                // This could be handled by a separate service if needed

                _logger.LogInformation($"Password successfully reset for user: {request.UserId}");

                return ApplicationResult<PasswordResetResponseDto>.Success(new PasswordResetResponseDto(
                    Success: true,
                    Message: "Password has been reset successfully. Please login with your new password."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PerformPasswordResetHandler");
                return ApplicationResult<PasswordResetResponseDto>.Failure("An error occurred resetting your password.");
            }
        }
    }
}