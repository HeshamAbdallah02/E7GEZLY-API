using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.LogoutAllDevices
{
    /// <summary>
    /// Handler for LogoutAllDevicesCommand using ITokenService
    /// </summary>
    public class LogoutAllDevicesHandler : IRequestHandler<LogoutAllDevicesCommand, ApplicationResult<object>>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<LogoutAllDevicesHandler> _logger;

        public LogoutAllDevicesHandler(ITokenService tokenService, ILogger<LogoutAllDevicesHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ApplicationResult<object>> Handle(LogoutAllDevicesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _tokenService.RevokeAllUserTokensAsync(request.UserId);
                if (!success)
                {
                    return ApplicationResult<object>.Failure("No active sessions found");
                }

                var response = new { message = "Logged out from all devices successfully" };
                return ApplicationResult<object>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout all devices for user {UserId}", request.UserId);
                return ApplicationResult<object>.Failure("An error occurred during logout");
            }
        }
    }
}