using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Queries
{
    /// <summary>
    /// Query for getting available password reset methods for a user
    /// </summary>
    public class GetAvailableResetMethodsQuery : IRequest<ApplicationResult<AvailableResetMethodsResponseDto>>
    {
        public string UserId { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for available reset methods
    /// </summary>
    public class AvailableResetMethodsResponseDto
    {
        public bool Success { get; init; }
        public List<ResetMethodInfo> AvailableMethods { get; init; } = new();
        public ResetMethodInfo? PreferredMethod { get; init; }
    }

    /// <summary>
    /// Information about a reset method
    /// </summary>
    public class ResetMethodInfo
    {
        public string Method { get; init; } = string.Empty;
        public int Value { get; init; }
        public string MaskedValue { get; init; } = string.Empty;
    }
}