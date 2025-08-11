using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.RevokeSession
{
    /// <summary>
    /// Command for revoking a specific session
    /// </summary>
    public class RevokeSessionCommand : IRequest<ApplicationResult<object>>
    {
        public string UserId { get; init; } = string.Empty;
        public Guid SessionId { get; init; }
    }
}