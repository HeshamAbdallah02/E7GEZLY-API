using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.SocialLogin
{
    /// <summary>
    /// Handler for social media authentication
    /// </summary>
    public class SocialLoginHandler : IRequestHandler<SocialLoginCommand, OperationResult<AuthResponseDto>>
    {
        private readonly ISocialAuthService _socialAuthService;
        private readonly ITokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SocialLoginHandler> _logger;

        public SocialLoginHandler(
            ISocialAuthService socialAuthService,
            ITokenService tokenService,
            UserManager<ApplicationUser> userManager,
            ILogger<SocialLoginHandler> logger)
        {
            _socialAuthService = socialAuthService;
            _tokenService = tokenService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<OperationResult<AuthResponseDto>> Handle(SocialLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate provider availability
                var availableProviders = _socialAuthService.GetAvailableProviders(IsAppleDevice(request.UserAgent));
                if (!availableProviders.Contains(request.Provider.ToLower()))
                {
                    return OperationResult<AuthResponseDto>.Failure($"Provider {request.Provider} is not available on this device");
                }

                // Validate token with provider
                var providerUser = await _socialAuthService.ValidateProviderTokenAsync(request.Provider, request.AccessToken);
                if (providerUser == null)
                {
                    return OperationResult<AuthResponseDto>.Failure("Invalid social media token");
                }

                // Find or create user
                var user = await _socialAuthService.FindOrCreateUserAsync(request.Provider, providerUser);
                if (user == null)
                {
                    return OperationResult<AuthResponseDto>.Failure("Failed to create or find user account");
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return OperationResult<AuthResponseDto>.Failure("Account is deactivated");
                }

                // Update last login for external login
                await _socialAuthService.UpdateExternalLoginAsync(user, request.Provider, providerUser.Id);

                // Create session info
                var sessionInfo = new CreateSessionDto(
                    DeviceName: request.DeviceName ?? $"{request.Provider} Login",
                    DeviceType: request.DeviceType ?? "Unknown",
                    UserAgent: request.UserAgent ?? "Unknown",
                    IpAddress: request.IpAddress ?? "Unknown"
                );

                // Generate tokens
                var authResponse = await _tokenService.GenerateTokensAsync(user, sessionInfo);

                _logger.LogInformation("Social login successful for user {UserId} via {Provider}", user.Id, request.Provider);

                return OperationResult<AuthResponseDto>.Success(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Social login error for provider {Provider}", request.Provider);
                return OperationResult<AuthResponseDto>.Failure("An error occurred during social login");
            }
        }

        private bool IsAppleDevice(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false;

            var appleDeviceIdentifiers = new[] { "iPhone", "iPad", "Mac", "Darwin" };
            return appleDeviceIdentifiers.Any(identifier =>
                userAgent.Contains(identifier, StringComparison.OrdinalIgnoreCase));
        }
    }
}