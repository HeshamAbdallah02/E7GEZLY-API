using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.Logout
{
    /// <summary>
    /// Handler for LogoutCommand using ITokenService
    /// </summary>
    public class LogoutHandler : IRequestHandler<LogoutCommand, ApplicationResult<object>>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<LogoutHandler> _logger;

        public LogoutHandler(ITokenService tokenService, ILogger<LogoutHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ApplicationResult<object>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return ApplicationResult<object>.Failure("No active session found");
                }

                var success = await _tokenService.RevokeTokenAsync(request.RefreshToken);
                if (!success)
                {
                    return ApplicationResult<object>.Failure("Failed to logout");
                }

                var response = new { message = "Logged out successfully" };
                return ApplicationResult<object>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return ApplicationResult<object>.Failure("An error occurred during logout");
            }
        }
    }
}