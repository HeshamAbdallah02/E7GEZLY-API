using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for CustomerProfile aggregate
    /// </summary>
    public class CustomerProfileRepository : ICustomerProfileRepository
    {
        private readonly AppDbContext _context;

        public CustomerProfileRepository(AppDbContext context)
        {
            _context = context;
        }

        // Query operations
        public async Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .FirstOrDefaultAsync(cp => cp.Id == id, cancellationToken);
        }

        public async Task<CustomerProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .FirstOrDefaultAsync(cp => cp.UserId == userId, cancellationToken);
        }

        public async Task<bool> ExistsForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles.AnyAsync(cp => cp.UserId == userId, cancellationToken);
        }

        // Search operations
        public async Task<IEnumerable<CustomerProfile>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(cp => (cp.Name.FirstName + " " + cp.Name.LastName).Contains(searchTerm))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CustomerProfile>> GetByDistrictAsync(int districtSystemId, CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(cp => cp.District!.SystemId == districtSystemId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CustomerProfile>> GetWithinRadiusAsync(Coordinates center, double radiusKm, CancellationToken cancellationToken = default)
        {
            // Using haversine formula for distance calculation
            var profiles = await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(cp => cp.Address.Coordinates != null)
                .ToListAsync(cancellationToken);

            return profiles
                .Where(cp => CalculateDistance(center.Latitude, center.Longitude, cp.Address.Coordinates!.Latitude, cp.Address.Coordinates.Longitude) <= radiusKm);
        }

        public async Task<IEnumerable<CustomerProfile>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken cancellationToken = default)
        {
            var minBirthDate = DateTime.Today.AddYears(-maxAge);
            var maxBirthDate = DateTime.Today.AddYears(-minAge);

            return await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(cp => cp.DateOfBirth >= minBirthDate && cp.DateOfBirth <= maxBirthDate)
                .ToListAsync(cancellationToken);
        }

        // Profile completeness
        public async Task<IEnumerable<CustomerProfile>> GetIncompleteProfilesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(cp => string.IsNullOrEmpty(cp.Name.FirstName) || 
                            string.IsNullOrEmpty(cp.Name.LastName) || 
                            cp.DateOfBirth == null ||
                            cp.DistrictSystemId == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CustomerProfile>> GetCompleteProfilesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(cp => !string.IsNullOrEmpty(cp.Name.FirstName) && 
                            !string.IsNullOrEmpty(cp.Name.LastName) && 
                            cp.DateOfBirth != null &&
                            cp.DistrictSystemId != null)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCompletionRateAsync(CancellationToken cancellationToken = default)
        {
            var totalCount = await _context.CustomerProfiles.CountAsync(cancellationToken);
            if (totalCount == 0) return 100;

            var completeCount = await _context.CustomerProfiles
                .CountAsync(cp => !string.IsNullOrEmpty(cp.Name.FirstName) && 
                                 !string.IsNullOrEmpty(cp.Name.LastName) && 
                                 cp.DateOfBirth != null &&
                                 cp.DistrictSystemId != null, cancellationToken);

            return (int)Math.Round((double)completeCount / totalCount * 100);
        }

        // Command operations
        public async Task<CustomerProfile> AddAsync(CustomerProfile customerProfile, CancellationToken cancellationToken = default)
        {
            var entry = await _context.CustomerProfiles.AddAsync(customerProfile, cancellationToken);
            return entry.Entity;
        }

        public async Task<CustomerProfile> UpdateAsync(CustomerProfile customerProfile, CancellationToken cancellationToken = default)
        {
            _context.CustomerProfiles.Update(customerProfile);
            return customerProfile;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var profile = await _context.CustomerProfiles.FindAsync(new object[] { id }, cancellationToken);
            if (profile != null)
            {
                _context.CustomerProfiles.Remove(profile);
            }
        }

        public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId, cancellationToken);
            if (profile != null)
            {
                _context.CustomerProfiles.Remove(profile);
            }
        }

        // Bulk operations
        public async Task<IEnumerable<CustomerProfile>> GetMultipleAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var profiles = await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(cp => ids.Contains(cp.Id))
                .ToListAsync(cancellationToken);

            return profiles;
        }

        public async Task<IEnumerable<CustomerProfile>> GetMultipleByUserIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
        {
            var profiles = await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(cp => userIds.Contains(cp.UserId))
                .ToListAsync(cancellationToken);

            return profiles;
        }

        public async Task<IEnumerable<CustomerProfile>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            var profiles = await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return profiles;
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles.CountAsync(cancellationToken);
        }

        // Statistics and reporting
        public async Task<Dictionary<int, int>> GetProfilesByDistrictAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CustomerProfiles
                .Where(cp => cp.DistrictSystemId.HasValue)
                .GroupBy(cp => cp.DistrictSystemId!.Value)
                .Select(g => new { DistrictId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DistrictId, x => x.Count, cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetAgeDistributionAsync(CancellationToken cancellationToken = default)
        {
            var profiles = await _context.CustomerProfiles
                .Where(cp => cp.DateOfBirth != null)
                .ToListAsync(cancellationToken);

            return profiles
                .GroupBy(cp => DateTime.Today.Year - cp.DateOfBirth!.Value.Year)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<IEnumerable<CustomerProfile>> GetRecentlyUpdatedAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var profiles = await _context.CustomerProfiles
                .Include(cp => cp.District)
                    .ThenInclude(d => d!.Governorate)
                .OrderByDescending(cp => cp.UpdatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return profiles;
        }

        public async Task<double> GetAverageAgeAsync(CancellationToken cancellationToken = default)
        {
            var ages = await _context.CustomerProfiles
                .Where(cp => cp.DateOfBirth != null)
                .Select(cp => DateTime.Today.Year - cp.DateOfBirth!.Value.Year)
                .ToListAsync(cancellationToken);

            return ages.Any() ? ages.Average() : 0;
        }

        // Location-based operations
        public async Task<IEnumerable<CustomerProfile>> GetCustomersNearVenueAsync(Guid venueId, double radiusKm = 5.0, CancellationToken cancellationToken = default)
        {
            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == venueId, cancellationToken);
            if (venue == null || venue.Address.Coordinates == null)
                return Enumerable.Empty<CustomerProfile>();

            return await GetWithinRadiusAsync(venue.Address.Coordinates, radiusKm, cancellationToken);
        }

        public async Task<int> GetCustomerCountInAreaAsync(Coordinates center, double radiusKm, CancellationToken cancellationToken = default)
        {
            var customersInRadius = await GetWithinRadiusAsync(center, radiusKm, cancellationToken);
            return customersInRadius.Count();
        }



        /// <summary>
        /// Calculate distance between two coordinates using Haversine formula
        /// </summary>
        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}