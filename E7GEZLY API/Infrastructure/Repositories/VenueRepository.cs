using AutoMapper;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for Venue aggregate
    /// Note: This is a transitional implementation during Clean Architecture migration.
    /// Methods will be implemented as needed to support existing functionality.
    /// </summary>
    public class VenueRepository : IVenueRepository
    {
        private readonly AppDbContext _context;

        public VenueRepository(AppDbContext context)
        {
            _context = context;
        }

        // Query operations
        public async Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var venue = await _context.Venues
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.WorkingHours)
                .Include(v => v.Pricing)
                .Include(v => v.Images)
                .Include(v => v.PlayStationDetails)
                .Include(v => v.SubUsers)
                .Include(v => v.AuditLogs)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

            return venue != null ? MapToDomainVenue(venue) : null;
        }

        public async Task<Venue?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            // In the E7GEZLY system, ApplicationUser has a VenueId property that establishes the relationship
            // We need to find the venue associated with this user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user?.VenueId == null)
                return null;

            var venue = await _context.Venues
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.WorkingHours)
                .Include(v => v.Pricing)
                .Include(v => v.Images)
                .Include(v => v.PlayStationDetails)
                .Include(v => v.SubUsers)
                .Include(v => v.AuditLogs)
                .FirstOrDefaultAsync(v => v.Id == user.VenueId.Value, cancellationToken);

            return venue != null ? MapToDomainVenue(venue) : null;
        }

        public async Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Venues.Where(v => v.Name == name);
            if (excludeId.HasValue)
            {
                query = query.Where(v => v.Id != excludeId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }

        // Command operations  
        public async Task<Venue> AddAsync(Venue venue, CancellationToken cancellationToken = default)
        {
            var efVenue = MapToEfVenue(venue);
            var entry = await _context.Venues.AddAsync(efVenue, cancellationToken);
            return MapToDomainVenue(entry.Entity);
        }

        public async Task<Venue> UpdateAsync(Venue venue, CancellationToken cancellationToken = default)
        {
            var efVenue = MapToEfVenue(venue);
            _context.Venues.Update(efVenue);
            return MapToDomainVenue(efVenue);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var venue = await _context.Venues.FindAsync(new object[] { id }, cancellationToken);
            if (venue != null)
            {
                _context.Venues.Remove(venue);
            }
        }

        // Basic bulk operations
        public async Task<IEnumerable<Venue>> GetMultipleAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var venues = await _context.Venues
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(v => ids.Contains(v.Id))
                .ToListAsync(cancellationToken);

            return venues.Select(v => MapToDomainVenue(v));
        }

        public async Task<IEnumerable<Venue>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            var venues = await _context.Venues
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return venues.Select(v => MapToDomainVenue(v));
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Venues.CountAsync(cancellationToken);
        }

        // Stub implementations for methods that need to be implemented later
        public Task<IEnumerable<Venue>> GetByTypeAsync(VenueType venueType, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetByTypeAsync will be implemented when needed");
        }

        public Task<IEnumerable<Venue>> GetByDistrictAsync(int districtSystemId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetByDistrictAsync will be implemented when needed");
        }

        public Task<IEnumerable<Venue>> GetWithinRadiusAsync(Coordinates center, double radiusKm, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetWithinRadiusAsync will be implemented when needed");
        }

        public Task<IEnumerable<Venue>> GetWithFeaturesAsync(VenueFeatures features, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetWithFeaturesAsync will be implemented when needed");
        }

        public Task<IEnumerable<Venue>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("SearchByNameAsync will be implemented when needed");
        }

        public Task<IEnumerable<Venue>> GetIncompleteProfilesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetIncompleteProfilesAsync will be implemented when needed");
        }

        public Task<IEnumerable<Venue>> GetCompletedProfilesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetCompletedProfilesAsync will be implemented when needed");
        }

        public Task<VenueSubUser?> GetSubUserByIdAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetSubUserByIdAsync will be implemented when needed");
        }

        public Task<VenueSubUser?> GetSubUserByUsernameAsync(Guid venueId, string username, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetSubUserByUsernameAsync will be implemented when needed");
        }

        public Task<bool> SubUserUsernameExistsInVenueAsync(Guid venueId, string username, Guid? excludeSubUserId = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("SubUserUsernameExistsInVenueAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueSubUser>> GetVenueSubUsersAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetVenueSubUsersAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueSubUser>> GetSubUsersByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            // Alias method for compatibility - delegates to GetVenueSubUsersAsync
            return GetVenueSubUsersAsync(venueId, false, cancellationToken);
        }

        public Task<VenueSubUser?> GetFounderAdminAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetFounderAdminAsync will be implemented when needed");
        }

        public Task<VenueSubUserSession?> GetSubUserSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetSubUserSessionAsync will be implemented when needed");
        }

        public Task<VenueSubUserSession?> GetActiveSubUserSessionByTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetActiveSubUserSessionByTokenAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueSubUserSession>> GetActiveSubUserSessionsAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetActiveSubUserSessionsAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueAuditLog>> GetAuditLogsAsync(Guid venueId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetAuditLogsAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueAuditLog>> GetAuditLogsByActionAsync(Guid venueId, string action, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetAuditLogsByActionAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueAuditLog>> GetAuditLogsBySubUserAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetAuditLogsBySubUserAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueAuditLog>> GetAuditLogsByDateRangeAsync(Guid venueId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetAuditLogsByDateRangeAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueWorkingHours>> GetWorkingHoursAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetWorkingHoursAsync will be implemented when needed");
        }

        public Task<VenueWorkingHours?> GetWorkingHoursForDayAsync(Guid venueId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetWorkingHoursForDayAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenuePricing>> GetPricingAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetPricingAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenuePricing>> GetPricingByTypeAsync(Guid venueId, PricingType pricingType, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetPricingByTypeAsync will be implemented when needed");
        }

        public Task<IEnumerable<VenueImage>> GetImagesAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetImagesAsync will be implemented when needed");
        }

        public Task<VenueImage?> GetPrimaryImageAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetPrimaryImageAsync will be implemented when needed");
        }

        public Task<VenuePlayStationDetails?> GetPlayStationDetailsAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetPlayStationDetailsAsync will be implemented when needed");
        }

        public Task CleanupExpiredSubUserSessionsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("CleanupExpiredSubUserSessionsAsync will be implemented when needed");
        }

        public Task EndAllSubUserSessionsAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("EndAllSubUserSessionsAsync will be implemented when needed");
        }

        public Task<Dictionary<VenueType, int>> GetVenueCountsByTypeAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetVenueCountsByTypeAsync will be implemented when needed");
        }

        public Task<int> GetProfileCompletionRateAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetProfileCompletionRateAsync will be implemented when needed");
        }

        public Task<IEnumerable<Venue>> GetMostPopularVenuesAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetMostPopularVenuesAsync will be implemented when needed");
        }

        public Task<Dictionary<string, int>> GetVenuesByDistrictAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetVenuesByDistrictAsync will be implemented when needed");
        }

        public Task<bool> CanCreateSubUserAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("CanCreateSubUserAsync will be implemented when needed");
        }

        public Task<int> GetSubUserCountAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GetSubUserCountAsync will be implemented when needed");
        }

        public Task<bool> HasActiveBookingsAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("HasActiveBookingsAsync will be implemented when needed");
        }

        // Simple mapping methods - to be enhanced with AutoMapper later
        private Venue MapToDomainVenue(Models.Venue efVenue)
        {
            return Venue.CreateExistingVenue(
                efVenue.Id,
                efVenue.Name,
                (Domain.Enums.VenueType)efVenue.VenueType,
                (Domain.Enums.VenueFeatures)efVenue.Features,
                efVenue.StreetAddress,
                efVenue.Landmark,
                efVenue.Latitude,
                efVenue.Longitude,
                efVenue.DistrictId,
                efVenue.IsProfileComplete,
                efVenue.RequiresSubUserSetup,
                efVenue.CreatedAt,
                efVenue.UpdatedAt
            );
        }

        private Models.Venue MapToEfVenue(Venue domainVenue)
        {
            return new Models.Venue
            {
                Id = domainVenue.Id,
                Name = domainVenue.Name.Name,
                VenueType = (VenueType)domainVenue.VenueType,
                Features = (VenueFeatures)domainVenue.Features,
                StreetAddress = domainVenue.Address.StreetAddress,
                Landmark = domainVenue.Address.Landmark,
                Latitude = domainVenue.Address.Coordinates?.Latitude,
                Longitude = domainVenue.Address.Coordinates?.Longitude,
                DistrictId = domainVenue.DistrictSystemId,
                PhoneNumber = domainVenue.PhoneNumber,
                WhatsAppNumber = domainVenue.WhatsAppNumber,
                FacebookUrl = domainVenue.FacebookUrl,
                InstagramUrl = domainVenue.InstagramUrl,
                Description = domainVenue.Description,
                IsActive = domainVenue.IsActive,
                IsProfileComplete = domainVenue.IsProfileComplete,
                RequiresSubUserSetup = domainVenue.RequiresSubUserSetup,
                CreatedAt = domainVenue.CreatedAt,
                UpdatedAt = domainVenue.UpdatedAt
            };
        }
    }
}