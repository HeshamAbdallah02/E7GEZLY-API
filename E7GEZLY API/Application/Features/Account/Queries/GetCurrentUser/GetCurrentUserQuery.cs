using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Queries.GetCurrentUser
{
    /// <summary>
    /// Query for getting current authenticated user profile with customer/venue details
    /// </summary>
    public class GetCurrentUserQuery : IRequest<ApplicationResult<object>>
    {
        public string UserId { get; init; } = string.Empty;
    }
}