using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Services.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Queries.GetActiveSessions
{
    /// <summary>
    /// Handler for GetActiveSessionsQuery using ITokenService
    /// </summary>
    public class GetActiveSessionsHandler : IRequestHandler<GetActiveSessionsQuery, ApplicationResult<SessionsResponseDto>>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<GetActiveSessionsHandler> _logger;

        public GetActiveSessionsHandler(ITokenService tokenService, ILogger<GetActiveSessionsHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ApplicationResult<SessionsResponseDto>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var sessions = await _tokenService.GetActiveSessionsAsync(request.UserId, request.CurrentRefreshToken);
                var response = new SessionsResponseDto(sessions, sessions.Count());
                
                return ApplicationResult<SessionsResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions for user {UserId}", request.UserId);
                return ApplicationResult<SessionsResponseDto>.Failure("An error occurred while retrieving active sessions");
            }
        }
    }
}