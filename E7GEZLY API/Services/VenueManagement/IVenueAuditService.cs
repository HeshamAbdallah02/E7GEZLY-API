using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.DTOs.Common;

namespace E7GEZLY_API.Services.VenueManagement
{
    /// <summary>
    /// Service for venue audit logging
    /// </summary>
    public interface IVenueAuditService
    {
        Task LogActionAsync(CreateAuditLogDto dto);

        Task LogVenueActionAsync(
            Guid venueId,
            string userId,
            string action,
            string description,
            object? additionalData = null);

        Task<PagedResult<VenueAuditLogResponseDto>> GetAuditLogsAsync(
            Guid venueId,
            VenueAuditLogQueryDto query);

        Task<Dictionary<string, object>> GetUserActivitySummaryAsync(
            Guid venueId,
            Guid subUserId,
            DateTime startDate,
            DateTime endDate);
    }

    public record CreateAuditLogDto(
        Guid VenueId,
        Guid? SubUserId,
        string Action,
        string EntityType,
        string EntityId,
        object? OldValues = null,
        object? NewValues = null,
        object? AdditionalData = null
    );
}