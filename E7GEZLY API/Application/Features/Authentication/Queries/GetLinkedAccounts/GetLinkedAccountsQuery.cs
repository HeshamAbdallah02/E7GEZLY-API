using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetLinkedAccounts
{
    /// <summary>
    /// Query to get linked social accounts for a user
    /// </summary>
    public class GetLinkedAccountsQuery : IRequest<OperationResult<LinkedAccountsResponseDto>>
    {
        public string UserId { get; set; } = string.Empty;
    }
}