using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace E7GEZLY_API.Application.Features.Account.Commands.ChangePassword
{
    /// <summary>
    /// Handler for ChangePasswordCommand with optional device logout
    /// </summary>
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ApplicationResult<object>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<ChangePasswordHandler> _logger;

        public ChangePasswordHandler(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ILogger<ChangePasswordHandler> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ApplicationResult<object>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return ApplicationResult<object>.Failure("User not found");
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApplicationResult<object>.Failure("Password change failed", errors);
                }

                // Optionally logout all devices if requested
                if (request.LogoutAllDevices)
                {
                    await _tokenService.RevokeAllUserTokensAsync(request.UserId);
                }

                _logger.LogInformation("Password changed for user: {UserId}", request.UserId);
                
                var response = new { message = "Password changed successfully" };
                return ApplicationResult<object>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
                return ApplicationResult<object>.Failure("An error occurred while changing password");
            }
        }
    }
}