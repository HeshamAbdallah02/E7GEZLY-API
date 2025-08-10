using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Authentication.Queries.ValidateToken
{
    /// <summary>
    /// Query for validating JWT token
    /// </summary>
    public class ValidateTokenQuery : IRequest<ApplicationResult<TokenValidationResultDto>>
    {
        public string Token { get; init; } = string.Empty;
        public bool IncludeUserDetails { get; init; } = false;
    }

    /// <summary>
    /// Response DTO for token validation
    /// </summary>
    public record TokenValidationResultDto
    {
        public bool IsValid { get; init; }
        public bool IsExpired { get; init; }
        public string? UserId { get; init; }
        public string? UserEmail { get; init; }
        public Guid? VenueId { get; init; }
        public List<string> Roles { get; init; } = new();
        public DateTime? ExpiresAt { get; init; }
        public string? Jti { get; init; }
        public string? Message { get; init; }
    }
}