using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.DTOs.Venue;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Queries.GetAuditLogs
{
    /// <summary>
    /// Query to get venue audit logs
    /// </summary>
    public record GetAuditLogsQuery : IRequest<PagedResult<VenueAuditLogResponseDto>>
    {
        public Guid VenueId { get; init; }
        public VenueAuditLogQueryDto QueryDto { get; init; } = null!;
    }
}