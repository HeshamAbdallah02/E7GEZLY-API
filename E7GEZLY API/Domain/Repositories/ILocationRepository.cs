using E7GEZLY_API.Domain.Entities;

namespace E7GEZLY_API.Domain.Repositories;

/// <summary>
/// Repository interface for Location entities (Governorate and District)
/// These are typically read-only reference data managed by the system
/// </summary>
public interface ILocationRepository
{
    // Governorate operations
    Task<IEnumerable<Governorate>> GetAllGovernoratesAsync(CancellationToken cancellationToken = default);
    Task<Governorate?> GetGovernorateByIdAsync(int systemId, CancellationToken cancellationToken = default);
    Task<Governorate?> GetGovernorateByNameAsync(string name, bool useArabic = false, CancellationToken cancellationToken = default);
    
    // District operations
    Task<IEnumerable<District>> GetAllDistrictsAsync(CancellationToken cancellationToken = default);
    Task<District?> GetDistrictByIdAsync(int systemId, CancellationToken cancellationToken = default);
    Task<District?> GetDistrictByNameAsync(string name, bool useArabic = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<District>> GetDistrictsByGovernorateAsync(int governorateSystemId, CancellationToken cancellationToken = default);
    
    // Search operations
    Task<IEnumerable<Governorate>> SearchGovernoratesAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<District>> SearchDistrictsAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    // Hierarchical operations
    Task<Governorate?> GetGovernorateWithDistrictsAsync(int governorateSystemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Governorate>> GetAllGovernoratesWithDistrictsAsync(CancellationToken cancellationToken = default);
    
    // Administrative operations (typically used for data seeding)
    Task<Governorate> AddGovernorateAsync(Governorate governorate, CancellationToken cancellationToken = default);
    Task<District> AddDistrictAsync(District district, CancellationToken cancellationToken = default);
    Task<IEnumerable<Governorate>> AddMultipleGovernoratesAsync(IEnumerable<Governorate> governorates, CancellationToken cancellationToken = default);
    Task<IEnumerable<District>> AddMultipleDistrictsAsync(IEnumerable<District> districts, CancellationToken cancellationToken = default);
    
    // Update operations (for corrections or updates to reference data)
    Task<Governorate> UpdateGovernorateAsync(Governorate governorate, CancellationToken cancellationToken = default);
    Task<District> UpdateDistrictAsync(District district, CancellationToken cancellationToken = default);
    
    // Validation operations
    Task<bool> GovernorateExistsAsync(int systemId, CancellationToken cancellationToken = default);
    Task<bool> DistrictExistsAsync(int systemId, CancellationToken cancellationToken = default);
    Task<bool> DistrictBelongsToGovernorateAsync(int districtSystemId, int governorateSystemId, CancellationToken cancellationToken = default);
    
    // Statistics
    Task<int> GetGovernorateCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetDistrictCountAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetDistrictCountsByGovernorateAsync(CancellationToken cancellationToken = default);

    // Convenience methods for Application layer
    Task<IEnumerable<Governorate>> GetGovernoratesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<District>> GetDistrictsAsync(int? governorateId = null, CancellationToken cancellationToken = default);
}