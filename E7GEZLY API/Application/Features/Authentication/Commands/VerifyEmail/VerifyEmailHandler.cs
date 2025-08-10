using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Domain.Services;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.VerifyEmail
{
    /// <summary>
    /// Handler for VerifyEmailCommand
    /// </summary>
    public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, ApplicationResult<VerificationResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IUserVerificationService _verificationService;
        private readonly ILogger<VerifyEmailHandler> _logger;

        public VerifyEmailHandler(
            UserManager<ApplicationUser> userManager,
            IUserRepository userRepository,
            IUserVerificationService verificationService,
            ILogger<VerifyEmailHandler> logger)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _verificationService = verificationService;
            _logger = logger;
        }

        public async Task<ApplicationResult<VerificationResponseDto>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Parse the user ID
                if (!Guid.TryParse(request.UserId, out var userId))
                {
                    return ApplicationResult<VerificationResponseDto>.Failure("Invalid user ID format");
                }
                
                // Get domain user entity
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return ApplicationResult<VerificationResponseDto>.Failure("User not found");
                }

                if (user.IsEmailVerified)
                {
                    var alreadyVerifiedResponse = new VerificationResponseDto
                    {
                        Success = true,
                        Message = "Email is already verified",
                        IsVerified = true
                    };
                    return ApplicationResult<VerificationResponseDto>.Success(alreadyVerifiedResponse);
                }

                // Use domain service to verify the code
                var isValidCode = await _verificationService.ValidateVerificationCodeAsync(
                    user,
                    request.VerificationCode,
                    Domain.Services.VerificationCodeType.EmailVerification
                );

                if (!isValidCode)
                {
                    return ApplicationResult<VerificationResponseDto>.Failure(
                        "Invalid or expired verification code");
                }

                // Use domain method to verify email
                user.VerifyEmail(request.VerificationCode);

                // Persist changes through repository
                await _userRepository.UpdateAsync(user);
                
                // Also update the ApplicationUser for Identity compatibility
                var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
                if (appUser != null)
                {
                    appUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(appUser);
                }

                _logger.LogInformation($"Email verified successfully for user {user.Id}");

                var response = new VerificationResponseDto
                {
                    Success = true,
                    Message = "Email verified successfully",
                    IsVerified = true
                };

                return ApplicationResult<VerificationResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying email for user {request.UserId}");
                return ApplicationResult<VerificationResponseDto>.Failure(
                    "An error occurred while verifying email");
            }
        }
    }
}