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
    /// Handler for ValidateResetCodeCommand
    /// </summary>
    public class ValidateResetCodeHandler : IRequestHandler<ValidateResetCodeCommand, ApplicationResult<ValidateResetCodeResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationService _verificationService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<ValidateResetCodeHandler> _logger;

        public ValidateResetCodeHandler(
            UserManager<ApplicationUser> userManager,
            IVerificationService verificationService,
            IDateTimeService dateTimeService,
            ILogger<ValidateResetCodeHandler> logger)
        {
            _userManager = userManager;
            _verificationService = verificationService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<ValidateResetCodeResponseDto>> Handle(ValidateResetCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return ApplicationResult<ValidateResetCodeResponseDto>.Success(new ValidateResetCodeResponseDto
                    {
                        IsValid = false,
                        Message = "Invalid user."
                    });
                }

                bool isValid = false;
                DateTime? expiry = null;
                bool? isUsed = null;

                if (request.Method == ResetMethod.Phone)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        request.ResetCode,
                        user.PhonePasswordResetCode,
                        user.PhonePasswordResetCodeExpiry);

                    expiry = user.PhonePasswordResetCodeExpiry;
                    isUsed = user.PhonePasswordResetCodeUsed;
                }
                else if (request.Method == ResetMethod.Email)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        request.ResetCode,
                        user.EmailPasswordResetCode,
                        user.EmailPasswordResetCodeExpiry);

                    expiry = user.EmailPasswordResetCodeExpiry;
                    isUsed = user.EmailPasswordResetCodeUsed;
                }

                if (isUsed == true)
                {
                    return ApplicationResult<ValidateResetCodeResponseDto>.Success(new ValidateResetCodeResponseDto
                    {
                        IsValid = false,
                        Message = "Reset code has already been used."
                    });
                }

                if (!isValid)
                {
                    return ApplicationResult<ValidateResetCodeResponseDto>.Success(new ValidateResetCodeResponseDto
                    {
                        IsValid = false,
                        Message = "Invalid or expired reset code."
                    });
                }

                var remainingMinutes = expiry.HasValue
                    ? (expiry.Value - _dateTimeService.UtcNow).TotalMinutes
                    : 0;

                return ApplicationResult<ValidateResetCodeResponseDto>.Success(new ValidateResetCodeResponseDto
                {
                    IsValid = true,
                    Message = "Reset code is valid.",
                    ExpiresInMinutes = Math.Round(remainingMinutes, 1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateResetCodeHandler");
                return ApplicationResult<ValidateResetCodeResponseDto>.Failure("An error occurred validating the reset code.");
            }
        }
    }
}