using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.ValueObjects;

namespace E7GEZLY_API.Domain.Repositories;

/// <summary>
/// Repository interface for Venue aggregate root
/// Defines data access operations for venues and their related entities
/// </summary>
public interface IVenueRepository
{
    // Query operations
    Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Venue?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    
    // Venue search and filtering
    Task<IEnumerable<Venue>> GetByTypeAsync(VenueType venueType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Venue>> GetByDistrictAsync(int districtSystemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Venue>> GetWithinRadiusAsync(Coordinates center, double radiusKm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Venue>> GetWithFeaturesAsync(VenueFeatures features, CancellationToken cancellationToken = default);
    Task<IEnumerable<Venue>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    // Profile completion status
    Task<IEnumerable<Venue>> GetIncompleteProfilesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Venue>> GetCompletedProfilesAsync(CancellationToken cancellationToken = default);
    
    // Sub-user operations
    Task<VenueSubUser?> GetSubUserByIdAsync(Guid subUserId, CancellationToken cancellationToken = default);
    Task<VenueSubUser?> GetSubUserByUsernameAsync(Guid venueId, string username, CancellationToken cancellationToken = default);
    Task<bool> SubUserUsernameExistsInVenueAsync(Guid venueId, string username, Guid? excludeSubUserId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<VenueSubUser>> GetVenueSubUsersAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<VenueSubUser>> GetSubUsersByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default); // Alias for compatibility
    Task<VenueSubUser?> GetFounderAdminAsync(Guid venueId, CancellationToken cancellationToken = default);
    
    // Sub-user sessions
    Task<VenueSubUserSession?> GetSubUserSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<VenueSubUserSession?> GetActiveSubUserSessionByTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<IEnumerable<VenueSubUserSession>> GetActiveSubUserSessionsAsync(Guid subUserId, CancellationToken cancellationToken = default);
    
    // Audit logs
    Task<IEnumerable<VenueAuditLog>> GetAuditLogsAsync(Guid venueId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<VenueAuditLog>> GetAuditLogsByActionAsync(Guid venueId, string action, CancellationToken cancellationToken = default);
    Task<IEnumerable<VenueAuditLog>> GetAuditLogsBySubUserAsync(Guid subUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<VenueAuditLog>> GetAuditLogsByDateRangeAsync(Guid venueId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    // Working hours
    Task<IEnumerable<VenueWorkingHours>> GetWorkingHoursAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<VenueWorkingHours?> GetWorkingHoursForDayAsync(Guid venueId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);
    
    // Pricing
    Task<IEnumerable<VenuePricing>> GetPricingAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<IEnumerable<VenuePricing>> GetPricingByTypeAsync(Guid venueId, PricingType pricingType, CancellationToken cancellationToken = default);
    
    // Images
    Task<IEnumerable<VenueImage>> GetImagesAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<VenueImage?> GetPrimaryImageAsync(Guid venueId, CancellationToken cancellationToken = default);
    
    // PlayStation details
    Task<VenuePlayStationDetails?> GetPlayStationDetailsAsync(Guid venueId, CancellationToken cancellationToken = default);

    // Command operations
    Task<Venue> AddAsync(Venue venue, CancellationToken cancellationToken = default);
    Task<Venue> UpdateAsync(Venue venue, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Bulk operations
    Task<IEnumerable<Venue>> GetMultipleAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IEnumerable<Venue>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    
    // Session cleanup
    Task CleanupExpiredSubUserSessionsAsync(CancellationToken cancellationToken = default);
    Task EndAllSubUserSessionsAsync(Guid subUserId, CancellationToken cancellationToken = default);
    
    // Statistics and reporting
    Task<Dictionary<VenueType, int>> GetVenueCountsByTypeAsync(CancellationToken cancellationToken = default);
    Task<int> GetProfileCompletionRateAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Venue>> GetMostPopularVenuesAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetVenuesByDistrictAsync(CancellationToken cancellationToken = default);
    
    // Specialized queries for business logic
    Task<bool> CanCreateSubUserAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<int> GetSubUserCountAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveBookingsAsync(Guid venueId, CancellationToken cancellationToken = default);
}