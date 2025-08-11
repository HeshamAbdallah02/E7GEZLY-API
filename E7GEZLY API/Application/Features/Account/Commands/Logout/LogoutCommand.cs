using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.Logout
{
    /// <summary>
    /// Command for logging out from current session
    /// </summary>
    public class LogoutCommand : IRequest<ApplicationResult<object>>
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}