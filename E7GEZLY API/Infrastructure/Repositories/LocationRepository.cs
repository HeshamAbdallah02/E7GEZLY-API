using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for Location entities (Governorates and Districts)
    /// </summary>
    public class LocationRepository : ILocationRepository
    {
        private readonly AppDbContext _context;

        public LocationRepository(AppDbContext context)
        {
            _context = context;
        }

        // Governorate operations
        public async Task<IEnumerable<Governorate>> GetAllGovernoratesAsync(CancellationToken cancellationToken = default)
        {
            var governorates = await _context.Governorates
                .Include(g => g.Districts)
                .OrderBy(g => g.NameEn)
                .ToListAsync(cancellationToken);

            return governorates.Select(MapToDomainGovernorate);
        }

        public async Task<Governorate?> GetGovernorateByIdAsync(int systemId, CancellationToken cancellationToken = default)
        {
            var governorate = await _context.Governorates
                .Include(g => g.Districts)
                .FirstOrDefaultAsync(g => g.Id == systemId, cancellationToken);

            return governorate != null ? MapToDomainGovernorate(governorate) : null;
        }

        public async Task<Governorate?> GetGovernorateByNameAsync(string name, bool useArabic = false, CancellationToken cancellationToken = default)
        {
            var governorate = await _context.Governorates
                .Include(g => g.Districts)
                .FirstOrDefaultAsync(g => useArabic ? g.NameAr == name : g.NameEn == name, cancellationToken);

            return governorate != null ? MapToDomainGovernorate(governorate) : null;
        }

        // District operations
        public async Task<IEnumerable<District>> GetAllDistrictsAsync(CancellationToken cancellationToken = default)
        {
            var districts = await _context.Districts
                .Include(d => d.Governorate)
                .OrderBy(d => d.NameEn)
                .ToListAsync(cancellationToken);

            return districts.Select(MapToDomainDistrict);
        }

        public async Task<District?> GetDistrictByIdAsync(int systemId, CancellationToken cancellationToken = default)
        {
            var district = await _context.Districts
                .Include(d => d.Governorate)
                .FirstOrDefaultAsync(d => d.Id == systemId, cancellationToken);

            return district != null ? MapToDomainDistrict(district) : null;
        }

        public async Task<District?> GetDistrictByNameAsync(string name, bool useArabic = false, CancellationToken cancellationToken = default)
        {
            var district = await _context.Districts
                .Include(d => d.Governorate)
                .FirstOrDefaultAsync(d => useArabic ? d.NameAr == name : d.NameEn == name, cancellationToken);

            return district != null ? MapToDomainDistrict(district) : null;
        }

        public async Task<IEnumerable<District>> GetDistrictsByGovernorateAsync(int governorateSystemId, CancellationToken cancellationToken = default)
        {
            var districts = await _context.Districts
                .Include(d => d.Governorate)
                .Where(d => d.GovernorateId == governorateSystemId)
                .OrderBy(d => d.NameEn)
                .ToListAsync(cancellationToken);

            return districts.Select(MapToDomainDistrict);
        }

        // Search operations
        public async Task<IEnumerable<Governorate>> SearchGovernoratesAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var governorates = await _context.Governorates
                .Include(g => g.Districts)
                .Where(g => g.NameEn.Contains(searchTerm) || g.NameAr.Contains(searchTerm))
                .OrderBy(g => g.NameEn)
                .ToListAsync(cancellationToken);

            return governorates.Select(MapToDomainGovernorate);
        }

        public async Task<IEnumerable<District>> SearchDistrictsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var districts = await _context.Districts
                .Include(d => d.Governorate)
                .Where(d => d.NameEn.Contains(searchTerm) || d.NameAr.Contains(searchTerm))
                .OrderBy(d => d.NameEn)
                .ToListAsync(cancellationToken);

            return districts.Select(MapToDomainDistrict);
        }

        // Hierarchical operations
        public async Task<Governorate?> GetGovernorateWithDistrictsAsync(int governorateSystemId, CancellationToken cancellationToken = default)
        {
            var governorate = await _context.Governorates
                .Include(g => g.Districts)
                .FirstOrDefaultAsync(g => g.Id == governorateSystemId, cancellationToken);

            return governorate != null ? MapToDomainGovernorate(governorate) : null;
        }

        public async Task<IEnumerable<Governorate>> GetAllGovernoratesWithDistrictsAsync(CancellationToken cancellationToken = default)
        {
            var governorates = await _context.Governorates
                .Include(g => g.Districts)
                .OrderBy(g => g.NameEn)
                .ToListAsync(cancellationToken);

            return governorates.Select(MapToDomainGovernorate);
        }

        // Administrative operations (typically used for data seeding)
        public async Task<Governorate> AddGovernorateAsync(Governorate governorate, CancellationToken cancellationToken = default)
        {
            var efGovernorate = MapToEfGovernorate(governorate);
            var entry = await _context.Governorates.AddAsync(efGovernorate, cancellationToken);
            return MapToDomainGovernorate(entry.Entity);
        }

        public async Task<District> AddDistrictAsync(District district, CancellationToken cancellationToken = default)
        {
            var efDistrict = MapToEfDistrict(district);
            var entry = await _context.Districts.AddAsync(efDistrict, cancellationToken);
            return MapToDomainDistrict(entry.Entity);
        }

        public async Task<IEnumerable<Governorate>> AddMultipleGovernoratesAsync(IEnumerable<Governorate> governorates, CancellationToken cancellationToken = default)
        {
            var efGovernorates = governorates.Select(MapToEfGovernorate);
            await _context.Governorates.AddRangeAsync(efGovernorates, cancellationToken);
            return efGovernorates.Select(MapToDomainGovernorate);
        }

        public async Task<IEnumerable<District>> AddMultipleDistrictsAsync(IEnumerable<District> districts, CancellationToken cancellationToken = default)
        {
            var efDistricts = districts.Select(MapToEfDistrict);
            await _context.Districts.AddRangeAsync(efDistricts, cancellationToken);
            return efDistricts.Select(MapToDomainDistrict);
        }

        // Update operations (for corrections or updates to reference data)
        public async Task<Governorate> UpdateGovernorateAsync(Governorate governorate, CancellationToken cancellationToken = default)
        {
            var efGovernorate = MapToEfGovernorate(governorate);
            _context.Governorates.Update(efGovernorate);
            return MapToDomainGovernorate(efGovernorate);
        }

        public async Task<District> UpdateDistrictAsync(District district, CancellationToken cancellationToken = default)
        {
            var efDistrict = MapToEfDistrict(district);
            _context.Districts.Update(efDistrict);
            return MapToDomainDistrict(efDistrict);
        }

        // Validation operations
        public async Task<bool> GovernorateExistsAsync(int systemId, CancellationToken cancellationToken = default)
        {
            return await _context.Governorates.AnyAsync(g => g.Id == systemId, cancellationToken);
        }

        public async Task<bool> DistrictExistsAsync(int systemId, CancellationToken cancellationToken = default)
        {
            return await _context.Districts.AnyAsync(d => d.Id == systemId, cancellationToken);
        }

        public async Task<bool> DistrictBelongsToGovernorateAsync(int districtSystemId, int governorateSystemId, CancellationToken cancellationToken = default)
        {
            return await _context.Districts
                .AnyAsync(d => d.Id == districtSystemId && d.GovernorateId == governorateSystemId, cancellationToken);
        }

        // Statistics
        public async Task<int> GetGovernorateCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Governorates.CountAsync(cancellationToken);
        }

        public async Task<int> GetDistrictCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Districts.CountAsync(cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetDistrictCountsByGovernorateAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Districts
                .GroupBy(d => d.GovernorateId)
                .Select(g => new { GovernorateId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GovernorateId, x => x.Count, cancellationToken);
        }

        // Convenience methods for Application layer
        public async Task<IEnumerable<Governorate>> GetGovernoratesAsync(CancellationToken cancellationToken = default)
        {
            return await GetAllGovernoratesAsync(cancellationToken);
        }

        public async Task<IEnumerable<District>> GetDistrictsAsync(int? governorateId = null, CancellationToken cancellationToken = default)
        {
            if (governorateId.HasValue)
            {
                return await GetDistrictsByGovernorateAsync(governorateId.Value, cancellationToken);
            }
            return await GetAllDistrictsAsync(cancellationToken);
        }

        // Mapping methods
        private Governorate MapToDomainGovernorate(Models.Governorate efGovernorate)
        {
            return Governorate.CreateExisting(
                Guid.NewGuid(), // Generate new GUID for domain entity
                efGovernorate.Id, // Use Id as SystemId
                efGovernorate.NameEn,
                efGovernorate.NameAr,
                DateTime.UtcNow, // Default created time
                DateTime.UtcNow  // Default updated time
            );
        }

        private District MapToDomainDistrict(Models.District efDistrict)
        {
            return District.CreateExisting(
                Guid.NewGuid(), // Generate new GUID for domain entity
                efDistrict.Id, // Use Id as SystemId
                efDistrict.NameEn,
                efDistrict.NameAr,
                efDistrict.GovernorateId, // Use GovernorateId as GovernorateSystemId
                efDistrict.CenterLatitude,
                efDistrict.CenterLongitude,
                DateTime.UtcNow, // Default created time
                DateTime.UtcNow  // Default updated time
            );
        }

        private Models.Governorate MapToEfGovernorate(Governorate domainGovernorate)
        {
            return new Models.Governorate
            {
                Id = domainGovernorate.SystemId, // Map SystemId to Id
                NameEn = domainGovernorate.NameEn,
                NameAr = domainGovernorate.NameAr
            };
        }

        private Models.District MapToEfDistrict(District domainDistrict)
        {
            return new Models.District
            {
                Id = domainDistrict.SystemId, // Map SystemId to Id
                NameEn = domainDistrict.NameEn,
                NameAr = domainDistrict.NameAr,
                GovernorateId = domainDistrict.GovernorateSystemId, // Map to GovernorateId
                CenterLatitude = domainDistrict.CenterCoordinates?.Latitude,
                CenterLongitude = domainDistrict.CenterCoordinates?.Longitude
            };
        }
    }
}