using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.ValueObjects;

namespace E7GEZLY_API.Domain.Repositories;

/// <summary>
/// Repository interface for CustomerProfile aggregate root
/// Defines data access operations for customer profiles
/// </summary>
public interface ICustomerProfileRepository
{
    // Query operations
    Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForUserAsync(string userId, CancellationToken cancellationToken = default);
    
    // Search operations
    Task<IEnumerable<CustomerProfile>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerProfile>> GetByDistrictAsync(int districtSystemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerProfile>> GetWithinRadiusAsync(Coordinates center, double radiusKm, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerProfile>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken cancellationToken = default);
    
    // Profile completeness
    Task<IEnumerable<CustomerProfile>> GetIncompleteProfilesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerProfile>> GetCompleteProfilesAsync(CancellationToken cancellationToken = default);
    Task<int> GetCompletionRateAsync(CancellationToken cancellationToken = default);

    // Command operations
    Task<CustomerProfile> AddAsync(CustomerProfile customerProfile, CancellationToken cancellationToken = default);
    Task<CustomerProfile> UpdateAsync(CustomerProfile customerProfile, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    // Bulk operations
    Task<IEnumerable<CustomerProfile>> GetMultipleAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerProfile>> GetMultipleByUserIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerProfile>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    
    // Statistics and reporting
    Task<Dictionary<int, int>> GetProfilesByDistrictAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetAgeDistributionAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerProfile>> GetRecentlyUpdatedAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<double> GetAverageAgeAsync(CancellationToken cancellationToken = default);
    
    // Location-based operations
    Task<IEnumerable<CustomerProfile>> GetCustomersNearVenueAsync(Guid venueId, double radiusKm = 5.0, CancellationToken cancellationToken = default);
    Task<int> GetCustomerCountInAreaAsync(Coordinates center, double radiusKm, CancellationToken cancellationToken = default);
}