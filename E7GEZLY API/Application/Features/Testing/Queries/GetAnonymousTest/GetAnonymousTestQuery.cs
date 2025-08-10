using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Testing.Queries.GetAnonymousTest
{
    /// <summary>
    /// Query for anonymous test endpoint
    /// </summary>
    public record GetAnonymousTestQuery : IRequest<ApplicationResult<AnonymousTestResponse>>;

    /// <summary>
    /// Response for anonymous test
    /// </summary>
    public record AnonymousTestResponse(string Message, DateTime Timestamp);
}