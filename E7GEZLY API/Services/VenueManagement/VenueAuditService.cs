// E7GEZLY API/Services/VenueManagement/VenueAuditService.cs
using Microsoft.EntityFrameworkCore;
using E7GEZLY_API.Data;
using E7GEZLY_API.DTOs.Venue;
using E7GEZLY_API.DTOs.Common;
using E7GEZLY_API.Models;
using System.Text.Json;

namespace E7GEZLY_API.Services.VenueManagement
{
    /// <summary>
    /// Implementation of venue audit logging service
    /// </summary>
    public class VenueAuditService : IVenueAuditService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<VenueAuditService> _logger;

        public VenueAuditService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<VenueAuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActionAsync(CreateAuditLogDto dto)
        {
            try
            {
                var auditLog = new VenueAuditLog
                {
                    VenueId = dto.VenueId,
                    SubUserId = dto.SubUserId,
                    Action = dto.Action,
                    EntityType = dto.EntityType,
                    EntityId = dto.EntityId,
                    OldValues = dto.OldValues != null ? JsonSerializer.Serialize(dto.OldValues) : null,
                    NewValues = dto.NewValues != null ? JsonSerializer.Serialize(dto.NewValues) : null,
                    AdditionalData = dto.AdditionalData != null ? JsonSerializer.Serialize(dto.AdditionalData) : null,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent()
                };

                _context.VenueAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Audit log created: {Action} on {EntityType} {EntityId} by SubUser {SubUserId}",
                    dto.Action, dto.EntityType, dto.EntityId, dto.SubUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create audit log for action {Action} on {EntityType} {EntityId}",
                    dto.Action, dto.EntityType, dto.EntityId);

                // Don't throw - audit logging failures shouldn't break business operations
            }
        }

        public async Task<PagedResult<VenueAuditLogResponseDto>> GetAuditLogsAsync(
            Guid venueId,
            VenueAuditLogQueryDto query)
        {
            var queryable = _context.VenueAuditLogs
                .Include(al => al.SubUser)
                .Where(al => al.VenueId == venueId);

            // Apply filters
            if (query.StartDate.HasValue)
            {
                queryable = queryable.Where(al => al.Timestamp >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                queryable = queryable.Where(al => al.Timestamp <= query.EndDate.Value);
            }

            if (query.SubUserId.HasValue)
            {
                queryable = queryable.Where(al => al.SubUserId == query.SubUserId.Value);
            }

            if (!string.IsNullOrEmpty(query.Action))
            {
                queryable = queryable.Where(al => al.Action.Contains(query.Action));
            }

            if (!string.IsNullOrEmpty(query.EntityType))
            {
                queryable = queryable.Where(al => al.EntityType == query.EntityType);
            }

            // Order by timestamp descending (newest first)
            queryable = queryable.OrderByDescending(al => al.Timestamp);

            // Get paged results
            var totalCount = await queryable.CountAsync();
            var logs = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var dtos = logs.Select(MapToResponseDto);

            return dtos.ToPagedResult(totalCount, query.Page, query.PageSize);
        }

        public async Task<Dictionary<string, object>> GetUserActivitySummaryAsync(
            Guid venueId,
            Guid subUserId,
            DateTime startDate,
            DateTime endDate)
        {
            var logs = await _context.VenueAuditLogs
                .Where(al => al.VenueId == venueId &&
                           al.SubUserId == subUserId &&
                           al.Timestamp >= startDate &&
                           al.Timestamp <= endDate)
                .GroupBy(al => al.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToListAsync();

            var summary = new Dictionary<string, object>
            {
                ["totalActions"] = logs.Sum(l => l.Count),
                ["actionBreakdown"] = logs.ToDictionary(l => l.Action, l => l.Count),
                ["periodStart"] = startDate,
                ["periodEnd"] = endDate,
                ["subUserId"] = subUserId
            };

            return summary;
        }

        #region Private Methods

        private VenueAuditLogResponseDto MapToResponseDto(VenueAuditLog auditLog)
        {
            return new VenueAuditLogResponseDto(
                auditLog.Id,
                auditLog.Action,
                auditLog.EntityType,
                auditLog.EntityId,
                auditLog.Timestamp,
                auditLog.SubUser?.Username,
                auditLog.IpAddress,
                ParseJsonValues(auditLog.OldValues),
                ParseJsonValues(auditLog.NewValues),
                ParseJsonValues(auditLog.AdditionalData)
            );
        }

        private Dictionary<string, object>? ParseJsonValues(string? jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON values: {Json}", jsonString);
                return new Dictionary<string, object> { ["raw"] = jsonString };
            }
        }

        private string? GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Check for forwarded headers first
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["User-Agent"].FirstOrDefault();
        }

        #endregion
    }
}