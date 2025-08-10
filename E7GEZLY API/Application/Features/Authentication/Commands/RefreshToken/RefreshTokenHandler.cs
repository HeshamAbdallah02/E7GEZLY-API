using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.RefreshToken
{
    /// <summary>
    /// Handler for RefreshTokenCommand
    /// </summary>
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, ApplicationResult<AuthResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<RefreshTokenHandler> _logger;

        public RefreshTokenHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            ITokenService tokenService,
            IDateTimeService dateTimeService,
            ILogger<RefreshTokenHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _tokenService = tokenService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find the user session with the refresh token
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.RefreshToken == request.RefreshToken && 
                                            s.IsActive &&
                                            s.RefreshTokenExpiry > _dateTimeService.UtcNow, 
                                       cancellationToken);

                if (userSession == null)
                {
                    return ApplicationResult<AuthResponseDto>.Failure("Invalid or expired refresh token");
                }

                // Get the user separately since domain entities don't have navigation properties
                var user = await _context.Users
                    .Include(u => u.Venue)
                    .FirstOrDefaultAsync(u => u.Id == userSession.UserId, cancellationToken);
                    
                if (user == null || !user.IsActive)
                {
                    return ApplicationResult<AuthResponseDto>.Failure("User not found or inactive");
                }

                // Check if user has required verifications
                if (!user.IsPhoneNumberVerified)
                {
                    return ApplicationResult<AuthResponseDto>.Failure("Phone number not verified");
                }

                // Generate new tokens using the new service method
                var tokenResponse = await _tokenService.GenerateTokensAsync(user);
                
                if (tokenResponse == null)
                {
                    return ApplicationResult<AuthResponseDto>.Failure("Failed to generate tokens");
                }

                var newRefreshToken = _tokenService.GenerateRefreshToken();

                // Update the session with new tokens
                var accessTokenExpiry = _dateTimeService.UtcNow.AddMinutes(60); // 1 hour
                var refreshTokenExpiry = _dateTimeService.UtcNow.AddDays(30); // 30 days
                
                // Update refresh token using domain method
                userSession.UpdateRefreshToken(newRefreshToken, refreshTokenExpiry);
                
                // Update device info using domain method
                userSession.UpdateDeviceInfo(request.DeviceName, request.DeviceType, 
                    request.UserAgent, request.IpAddress);

                await _context.SaveChangesAsync(cancellationToken);

                // Create user info dictionary
                var userInfo = new Dictionary<string, string>
                {
                    ["Id"] = user.Id,
                    ["Email"] = user.Email!,
                    ["PhoneNumber"] = user.PhoneNumber ?? "",
                    ["IsPhoneVerified"] = user.IsPhoneNumberVerified.ToString(),
                    ["IsEmailVerified"] = user.IsEmailVerified.ToString()
                };
                
                if (user.VenueId.HasValue)
                {
                    userInfo["VenueId"] = user.VenueId.ToString()!;
                }
                
                // Add venue info if available
                if (user.Venue != null)
                {
                    userInfo["VenueName"] = user.Venue.Name;
                    userInfo["VenueType"] = user.Venue.VenueType.ToString();
                    userInfo["IsVenueProfileComplete"] = user.Venue.IsProfileComplete.ToString();
                }

                // Use the response from the token service but with our custom refresh token
                var response = new AuthResponseDto(
                    AccessToken: tokenResponse.AccessToken,
                    RefreshToken: newRefreshToken,
                    AccessTokenExpiry: tokenResponse.AccessTokenExpiry,
                    UserType: tokenResponse.UserType,
                    UserInfo: tokenResponse.UserInfo
                );

                _logger.LogInformation($"Token refreshed for user {user.Id}");

                return ApplicationResult<AuthResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return ApplicationResult<AuthResponseDto>.Failure("An error occurred during token refresh");
            }
        }
    }
}