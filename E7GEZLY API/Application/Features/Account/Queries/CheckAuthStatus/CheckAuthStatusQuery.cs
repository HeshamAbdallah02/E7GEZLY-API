using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Queries.CheckAuthStatus
{
    /// <summary>
    /// Query for checking authentication status
    /// </summary>
    public class CheckAuthStatusQuery : IRequest<ApplicationResult<object>>
    {
        public string UserId { get; init; } = string.Empty;
    }
}