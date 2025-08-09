using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.Services.VenueManagement;
using MediatR;

namespace E7GEZLY_API.Application.Features.SubUsers.Queries.GetAuditLogs
{
    /// <summary>
    /// Handler for getting venue audit logs
    /// </summary>
    public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<VenueAuditLogResponseDto>>
    {
        private readonly IVenueAuditService _auditService;

        public GetAuditLogsHandler(IVenueAuditService auditService)
        {
            _auditService = auditService;
        }

        public async Task<PagedResult<VenueAuditLogResponseDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            return await _auditService.GetAuditLogsAsync(request.VenueId, request.QueryDto);
        }
    }
}