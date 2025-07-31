namespace E7GEZLY_API.DTOs.Venue
{
    /// <summary>
    /// Query parameters for audit logs
    /// </summary>
    public record VenueAuditLogQueryDto(
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        Guid? SubUserId = null,
        string? Action = null,
        string? EntityType = null,
        int Page = 1,
        int PageSize = 50
    );

    /// <summary>
    /// Response DTO for audit log entry
    /// </summary>
    public record VenueAuditLogResponseDto(
        Guid Id,
        string Action,
        string EntityType,
        string EntityId,
        DateTime Timestamp,
        string? SubUserUsername,
        string? IpAddress,
        Dictionary<string, object>? OldValues,
        Dictionary<string, object>? NewValues,
        Dictionary<string, object>? AdditionalData
    );
}