using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Commands.DeactivateAccount
{
    /// <summary>
    /// Command for deactivating user account
    /// </summary>
    public class DeactivateAccountCommand : IRequest<ApplicationResult<object>>
    {
        public string UserId { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? Reason { get; init; }
    }
}