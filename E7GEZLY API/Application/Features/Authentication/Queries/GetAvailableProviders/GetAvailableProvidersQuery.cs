using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.DTOs.Auth;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.GetAvailableProviders
{
    /// <summary>
    /// Query to get available social authentication providers
    /// </summary>
    public class GetAvailableProvidersQuery : IRequest<OperationResult<AvailableProvidersDto>>
    {
        public string? UserAgent { get; set; }
    }
}