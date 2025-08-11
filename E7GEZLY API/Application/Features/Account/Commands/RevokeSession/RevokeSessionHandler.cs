using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.Services.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.RevokeSession
{
    /// <summary>
    /// Handler for RevokeSessionCommand using ITokenService
    /// </summary>
    public class RevokeSessionHandler : IRequestHandler<RevokeSessionCommand, ApplicationResult<object>>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<RevokeSessionHandler> _logger;

        public RevokeSessionHandler(ITokenService tokenService, ILogger<RevokeSessionHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ApplicationResult<object>> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _tokenService.RevokeSessionAsync(request.UserId, request.SessionId);
                if (!success)
                {
                    return ApplicationResult<object>.Failure("Session not found");
                }

                var response = new { message = "Session revoked successfully" };
                return ApplicationResult<object>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking session {SessionId} for user {UserId}", request.SessionId, request.UserId);
                return ApplicationResult<object>.Failure("An error occurred while revoking session");
            }
        }
    }
}