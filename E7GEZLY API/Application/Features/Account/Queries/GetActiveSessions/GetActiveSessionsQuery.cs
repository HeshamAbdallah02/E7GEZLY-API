using E7GEZLY_API.Application.Common.Models;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Queries.GetActiveSessions
{
    /// <summary>
    /// Query for getting active sessions for a user
    /// </summary>
    public class GetActiveSessionsQuery : IRequest<ApplicationResult<SessionsResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
        public string? CurrentRefreshToken { get; init; }
    }
}