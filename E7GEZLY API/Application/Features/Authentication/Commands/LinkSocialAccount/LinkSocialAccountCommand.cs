using E7GEZLY_API.Application.Common.Interfaces;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Commands.LinkSocialAccount
{
    /// <summary>
    /// Command for linking social account to existing user account
    /// </summary>
    public class LinkSocialAccountCommand : IRequest<OperationResult<string>>
    {
        public string UserId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
    }
}