using E7GEZLY_API.Application.Common.Interfaces;
using MediatR;

namespace E7GEZLY_API.Application.Features.VenueProfile.Queries.IsProfileComplete
{
    /// <summary>
    /// Query for checking if venue profile is complete
    /// </summary>
    public class IsProfileCompleteQuery : IRequest<OperationResult<ProfileCompletionStatusDto>>
    {
        public Guid VenueId { get; init; }
    }

    /// <summary>
    /// Response DTO for profile completion status
    /// </summary>
    public class ProfileCompletionStatusDto
    {
        public bool IsComplete { get; init; }
        public List<string> MissingFields { get; init; } = new();
        public List<string> CompletedSections { get; init; } = new();
        public List<string> RequiredSections { get; init; } = new();
        public decimal CompletionPercentage { get; init; }
        public string VenueType { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}