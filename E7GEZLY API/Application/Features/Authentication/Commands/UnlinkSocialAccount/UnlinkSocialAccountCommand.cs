using E7GEZLY_API.Application.Common.Interfaces;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.UnlinkSocialAccount
{
    /// <summary>
    /// Command for unlinking social account from user account
    /// </summary>
    public class UnlinkSocialAccountCommand : IRequest<OperationResult<string>>
    {
        public string UserId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
}