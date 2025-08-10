using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.VerifyAccount
{
    /// <summary>
    /// Handler for VerifyAccountCommand
    /// </summary>
    public class VerifyAccountHandler : IRequestHandler<VerifyAccountCommand, ApplicationResult<VerifyAccountResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationService _verificationService;
        private readonly ITokenService _tokenService;
        private readonly AppDbContext _context;
        private readonly ILogger<VerifyAccountHandler> _logger;

        public VerifyAccountHandler(
            UserManager<ApplicationUser> userManager,
            IVerificationService verificationService,
            ITokenService tokenService,
            AppDbContext context,
            ILogger<VerifyAccountHandler> logger)
        {
            _userManager = userManager;
            _verificationService = verificationService;
            _tokenService = tokenService;
            _context = context;
            _logger = logger;
        }

        public async Task<ApplicationResult<VerifyAccountResponseDto>> Handle(VerifyAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return ApplicationResult<VerifyAccountResponseDto>.Failure("User not found");
                }

                bool isValid = false;

                if (request.Method == VerificationMethod.Phone)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        request.VerificationCode,
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
                else if (request.Method == VerificationMethod.Email)
                {
                    isValid = await _verificationService.ValidateVerificationCodeAsync(
                        request.VerificationCode,
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

                if (!isValid)
                {
                    return ApplicationResult<VerifyAccountResponseDto>.Failure("Invalid or expired verification code");
                }

                await _userManager.UpdateAsync(user);
                _logger.LogInformation($"Account verified for user {user.Id} via {request.Method}");

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

                    var response = new VerifyAccountResponseDto
                    {
                        Success = true,
                        Message = "Account verified successfully",
                        Tokens = tokens,
                        VenueInfo = new VenueInfoDto
                        {
                            VenueId = venue?.Id,
                            VenueName = venue?.Name?.Name,
                            VenueType = venue?.VenueType.ToString(),
                            IsProfileComplete = venue?.IsProfileComplete ?? false
                        },
                        RequiredActions = requiredActions,
                        Metadata = metadata
                    };

                    return ApplicationResult<VerifyAccountResponseDto>.Success(response);
                }

                // Customer verification response
                var customerResponse = new VerifyAccountResponseDto
                {
                    Success = true,
                    Message = "Account verified successfully",
                    Tokens = tokens,
                    UserType = "customer"
                };

                return ApplicationResult<VerifyAccountResponseDto>.Success(customerResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during account verification");
                return ApplicationResult<VerifyAccountResponseDto>.Failure("An error occurred during verification");
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
    }
}