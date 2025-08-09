using AutoMapper;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

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

        public async Task<VenueSubUser?> GetSubUserByIdAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            var efSubUser = await _context.VenueSubUsers
                .Include(su => su.Venue)
                .FirstOrDefaultAsync(su => su.Id == subUserId, cancellationToken);

            return efSubUser != null ? MapToDomainVenueSubUser(efSubUser) : null;
        }

        public async Task<VenueSubUser?> GetSubUserByUsernameAsync(Guid venueId, string username, CancellationToken cancellationToken = default)
        {
            var efSubUser = await _context.VenueSubUsers
                .Include(su => su.Venue)
                .FirstOrDefaultAsync(su => su.VenueId == venueId && su.Username == username, cancellationToken);

            return efSubUser != null ? MapToDomainVenueSubUser(efSubUser) : null;
        }

        public async Task<bool> SubUserUsernameExistsInVenueAsync(Guid venueId, string username, Guid? excludeSubUserId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.VenueSubUsers
                .Where(su => su.VenueId == venueId && su.Username == username);

            if (excludeSubUserId.HasValue)
            {
                query = query.Where(su => su.Id != excludeSubUserId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<IEnumerable<VenueSubUser>> GetVenueSubUsersAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.VenueSubUsers
                .Include(su => su.Venue)
                .Where(su => su.VenueId == venueId);

            if (!includeInactive)
            {
                query = query.Where(su => su.IsActive);
            }

            var efSubUsers = await query.ToListAsync(cancellationToken);
            return efSubUsers.Select(su => MapToDomainVenueSubUser(su));
        }

        public Task<IEnumerable<VenueSubUser>> GetSubUsersByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            // Alias method for compatibility - delegates to GetVenueSubUsersAsync
            return GetVenueSubUsersAsync(venueId, false, cancellationToken);
        }

        public async Task<VenueSubUser?> GetFounderAdminAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            var efSubUser = await _context.VenueSubUsers
                .Include(su => su.Venue)
                .FirstOrDefaultAsync(su => su.VenueId == venueId && su.IsFounderAdmin, cancellationToken);

            return efSubUser != null ? MapToDomainVenueSubUser(efSubUser) : null;
        }

        public async Task<VenueSubUserSession?> GetSubUserSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var efSession = await _context.VenueSubUserSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            return efSession != null ? MapToDomainVenueSubUserSession(efSession) : null;
        }

        public async Task<VenueSubUserSession?> GetActiveSubUserSessionByTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var efSession = await _context.VenueSubUserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && 
                                         s.IsActive && 
                                         s.RefreshTokenExpiry > DateTime.UtcNow, cancellationToken);

            return efSession != null ? MapToDomainVenueSubUserSession(efSession) : null;
        }

        public async Task<IEnumerable<VenueSubUserSession>> GetActiveSubUserSessionsAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            var efSessions = await _context.VenueSubUserSessions
                .Where(s => s.SubUserId == subUserId && s.IsActive && s.RefreshTokenExpiry > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            return efSessions.Select(s => MapToDomainVenueSubUserSession(s));
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

        public async Task EndAllSubUserSessionsAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            var sessions = await _context.VenueSubUserSessions
                .Where(s => s.SubUserId == subUserId && s.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LogoutAt = DateTime.UtcNow;
                session.LogoutReason = "Admin logout";
                session.UpdatedAt = DateTime.UtcNow;
            }
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

        // Sub-user mapping methods
        private Domain.Entities.VenueSubUser MapToDomainVenueSubUser(Models.VenueSubUser efSubUser)
        {
            // Use reflection to create the domain entity since constructors are private
            var domainSubUser = Domain.Entities.VenueSubUser.Create(
                efSubUser.VenueId,
                efSubUser.Username,
                efSubUser.PasswordHash,
                efSubUser.Role,
                efSubUser.Permissions,
                efSubUser.CreatedBySubUserId ?? Guid.Empty
            );

            // Set additional properties using reflection
            SetPrivateProperty(domainSubUser, nameof(Domain.Entities.VenueSubUser.Id), efSubUser.Id);
            SetPrivateProperty(domainSubUser, nameof(Domain.Entities.VenueSubUser.IsActive), efSubUser.IsActive);
            SetPrivateProperty(domainSubUser, nameof(Domain.Entities.VenueSubUser.IsFounderAdmin), efSubUser.IsFounderAdmin);
            SetPrivateProperty(domainSubUser, nameof(Domain.Entities.VenueSubUser.LastLoginAt), efSubUser.LastLoginAt);
            SetPrivateProperty(domainSubUser, nameof(Domain.Entities.VenueSubUser.FailedLoginAttempts), efSubUser.FailedLoginAttempts);
            SetPrivateProperty(domainSubUser, nameof(Domain.Entities.VenueSubUser.LockoutEnd), efSubUser.LockoutEnd);
            SetPrivateProperty(domainSubUser, nameof(Domain.Entities.VenueSubUser.PasswordChangedAt), efSubUser.PasswordChangedAt);
            SetPrivateProperty(domainSubUser, nameof(Domain.Entities.VenueSubUser.MustChangePassword), efSubUser.MustChangePassword);
            SetPrivateProperty(domainSubUser, "CreatedAt", efSubUser.CreatedAt);
            SetPrivateProperty(domainSubUser, "UpdatedAt", efSubUser.UpdatedAt);

            return domainSubUser;
        }

        private Domain.Entities.VenueSubUserSession MapToDomainVenueSubUserSession(Models.VenueSubUserSession efSession)
        {
            var domainSession = Domain.Entities.VenueSubUserSession.Create(
                efSession.SubUserId,
                efSession.RefreshToken,
                efSession.RefreshTokenExpiry,
                efSession.DeviceName,
                efSession.DeviceType,
                efSession.IpAddress,
                efSession.UserAgent,
                efSession.AccessTokenJti
            );

            // Set additional properties using reflection
            SetPrivateProperty(domainSession, nameof(Domain.Entities.VenueSubUserSession.Id), efSession.Id);
            SetPrivateProperty(domainSession, nameof(Domain.Entities.VenueSubUserSession.IsActive), efSession.IsActive);
            SetPrivateProperty(domainSession, nameof(Domain.Entities.VenueSubUserSession.LastActivityAt), efSession.LastActivityAt);
            SetPrivateProperty(domainSession, nameof(Domain.Entities.VenueSubUserSession.LogoutAt), efSession.LogoutAt);
            SetPrivateProperty(domainSession, nameof(Domain.Entities.VenueSubUserSession.LogoutReason), efSession.LogoutReason);
            SetPrivateProperty(domainSession, "CreatedAt", efSession.CreatedAt);
            SetPrivateProperty(domainSession, "UpdatedAt", efSession.UpdatedAt);

            return domainSession;
        }

        private static void SetPrivateProperty<T>(T obj, string propertyName, object? value)
        {
            var property = typeof(T).GetProperty(propertyName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);
            
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
        }
    }
}