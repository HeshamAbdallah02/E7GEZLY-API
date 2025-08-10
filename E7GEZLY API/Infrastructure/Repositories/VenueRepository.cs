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
        public async Task<Domain.Entities.Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var efVenue = await _context.Venues
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.WorkingHours)
                .Include(v => v.Pricing)
                .Include(v => v.Images)
                .Include(v => v.PlayStationDetails)
                .Include(v => v.SubUsers)
                .Include(v => v.AuditLogs)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

            return efVenue;
        }

        public async Task<Domain.Entities.Venue?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            // In the E7GEZLY system, ApplicationUser has a VenueId property that establishes the relationship
            // We need to find the venue associated with this user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user?.VenueId == null)
                return null;

            var efVenue = await _context.Venues
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.WorkingHours)
                .Include(v => v.Pricing)
                .Include(v => v.Images)
                .Include(v => v.PlayStationDetails)
                .Include(v => v.SubUsers)
                .Include(v => v.AuditLogs)
                .FirstOrDefaultAsync(v => v.Id == user.VenueId.Value, cancellationToken);

            return efVenue;
        }

        public async Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Venues.Where(v => v.Name.Name == name);
            if (excludeId.HasValue)
            {
                query = query.Where(v => v.Id != excludeId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }

        // Command operations  
        public async Task<Domain.Entities.Venue> AddAsync(Domain.Entities.Venue venue, CancellationToken cancellationToken = default)
        {
            // Domain entities are used directly with EF Core
            var entry = await _context.Venues.AddAsync(venue, cancellationToken);
            return entry.Entity;
        }

        public async Task<Domain.Entities.Venue> UpdateAsync(Domain.Entities.Venue venue, CancellationToken cancellationToken = default)
        {
            // Domain entities are used directly with EF Core
            _context.Venues.Update(venue);
            return venue;
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
        public async Task<IEnumerable<Domain.Entities.Venue>> GetMultipleAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var efVenues = await _context.Venues
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Where(v => ids.Contains(v.Id))
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<IEnumerable<Domain.Entities.Venue>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            var efVenues = await _context.Venues
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Venues.CountAsync(cancellationToken);
        }

        // Venue search and filtering implementations
        public async Task<IEnumerable<Domain.Entities.Venue>> GetByTypeAsync(VenueType venueType, CancellationToken cancellationToken = default)
        {
            var efVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && v.VenueType == venueType)
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.Images.Where(vi => vi.IsPrimary))
                .Include(v => v.WorkingHours)
                .OrderBy(v => v.Name)
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<IEnumerable<Domain.Entities.Venue>> GetByDistrictAsync(int districtSystemId, CancellationToken cancellationToken = default)
        {
            var efVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && v.DistrictSystemId == districtSystemId)
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.Images.Where(vi => vi.IsPrimary))
                .Include(v => v.WorkingHours)
                .OrderBy(v => v.Name)
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<IEnumerable<Domain.Entities.Venue>> GetWithinRadiusAsync(Coordinates center, double radiusKm, CancellationToken cancellationToken = default)
        {
            if (radiusKm <= 0)
                throw new ArgumentException("Radius must be greater than zero", nameof(radiusKm));

            // Convert to raw coordinates for the query
            var centerLat = center.Latitude;
            var centerLng = center.Longitude;
            
            // Pre-filter venues using a bounding box for performance (faster than calculating distance for all venues)
            // Approximate degrees per kilometer (varies by latitude, but close enough for Egypt)
            var latDelta = radiusKm / 111.0; // ~111 km per degree latitude
            var lngDelta = radiusKm / (111.0 * Math.Cos(centerLat * Math.PI / 180)); // Adjusted for longitude at this latitude
            
            var efVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && 
                           v.Address.Coordinates != null &&
                           v.Address.Coordinates.Latitude >= centerLat - latDelta && v.Address.Coordinates.Latitude <= centerLat + latDelta &&
                           v.Address.Coordinates.Longitude >= centerLng - lngDelta && v.Address.Coordinates.Longitude <= centerLng + lngDelta)
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.Images.Where(vi => vi.IsPrimary))
                .Include(v => v.WorkingHours)
                .ToListAsync(cancellationToken);

            // Calculate precise distance using Haversine formula and filter
            var venuesWithDistance = efVenues
                .Select(v => new { 
                    Venue = v, 
                    Distance = CalculateDistance(centerLat, centerLng, v.Address.Coordinates!.Latitude, v.Address.Coordinates.Longitude) 
                })
                .Where(vd => vd.Distance <= radiusKm)
                .OrderBy(vd => vd.Distance)
                .Select(vd => vd.Venue);

            return venuesWithDistance;
        }

        /// <summary>
        /// Calculate distance between two points using Haversine formula
        /// Optimized for Egyptian geographic coordinates
        /// </summary>
        private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371;
            
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadiusKm * c;
        }

        public async Task<IEnumerable<Domain.Entities.Venue>> GetWithFeaturesAsync(VenueFeatures features, CancellationToken cancellationToken = default)
        {
            var efVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && 
                           (v.Features & features) == features)
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.Images.Where(vi => vi.IsPrimary))
                .Include(v => v.WorkingHours)
                .OrderBy(v => v.Name.Name)
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<IEnumerable<Domain.Entities.Venue>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<Domain.Entities.Venue>();

            // Use contains for case-insensitive search (works with Arabic and English)
            var efVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && 
                           (v.Name.Name.Contains(searchTerm) ||
                            (v.Description != null && v.Description.Contains(searchTerm)) ||
                            (v.Address.StreetAddress != null && v.Address.StreetAddress.Contains(searchTerm)) ||
                            (v.Address.Landmark != null && v.Address.Landmark.Contains(searchTerm))))
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.Images.Where(vi => vi.IsPrimary))
                .OrderBy(v => v.Name.Name)
                .Take(50) // Limit results for performance
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<IEnumerable<Domain.Entities.Venue>> GetIncompleteProfilesAsync(CancellationToken cancellationToken = default)
        {
            var efVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && !v.IsProfileComplete)
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.Images.Where(vi => vi.IsPrimary))
                .Include(v => v.WorkingHours)
                .Include(v => v.Pricing)
                .OrderBy(v => v.CreatedAt) // Show oldest incomplete profiles first
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<IEnumerable<Domain.Entities.Venue>> GetCompletedProfilesAsync(CancellationToken cancellationToken = default)
        {
            var efVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && v.IsProfileComplete)
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.Images.Where(vi => vi.IsPrimary))
                .Include(v => v.WorkingHours)
                .Include(v => v.Pricing)
                .OrderByDescending(v => v.UpdatedAt) // Show recently updated profiles first
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<Domain.Entities.VenueSubUser?> GetSubUserByIdAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            var efSubUser = await _context.VenueSubUsers
                .FirstOrDefaultAsync(su => su.Id == subUserId, cancellationToken);

            return efSubUser;
        }

        public async Task<Domain.Entities.VenueSubUser?> GetSubUserByUsernameAsync(Guid venueId, string username, CancellationToken cancellationToken = default)
        {
            var efSubUser = await _context.VenueSubUsers
                .FirstOrDefaultAsync(su => su.VenueId == venueId && su.Username == username, cancellationToken);

            return efSubUser;
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

        public async Task<IEnumerable<Domain.Entities.VenueSubUser>> GetVenueSubUsersAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.VenueSubUsers
                .Where(su => su.VenueId == venueId);

            if (!includeInactive)
            {
                query = query.Where(su => su.IsActive);
            }

            var efSubUsers = await query.ToListAsync(cancellationToken);
            return efSubUsers;
        }

        public Task<IEnumerable<Domain.Entities.VenueSubUser>> GetSubUsersByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            // Alias method for compatibility - delegates to GetVenueSubUsersAsync
            return GetVenueSubUsersAsync(venueId, false, cancellationToken);
        }

        public async Task<Domain.Entities.VenueSubUser?> GetFounderAdminAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            var efFounderAdmin = await _context.VenueSubUsers
                .FirstOrDefaultAsync(su => su.VenueId == venueId && su.IsFounderAdmin, cancellationToken);

            return efFounderAdmin;
        }

        public async Task<Domain.Entities.VenueSubUserSession?> GetSubUserSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var efSession = await _context.VenueSubUserSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            return efSession;
        }

        public async Task<Domain.Entities.VenueSubUserSession?> GetActiveSubUserSessionByTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var efSession = await _context.VenueSubUserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && 
                                         s.IsActive && 
                                         s.RefreshTokenExpiry > DateTime.UtcNow, cancellationToken);

            return efSession;
        }

        public async Task<IEnumerable<Domain.Entities.VenueSubUserSession>> GetActiveSubUserSessionsAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            var efSessions = await _context.VenueSubUserSessions
                .Where(s => s.SubUserId == subUserId && s.IsActive && s.RefreshTokenExpiry > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            return efSessions;
        }

        public async Task<IEnumerable<Domain.Entities.VenueAuditLog>> GetAuditLogsAsync(Guid venueId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            var efAuditLogs = await _context.VenueAuditLogs
                .Where(al => al.VenueId == venueId)
                .OrderByDescending(al => al.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return efAuditLogs;
        }

        public async Task<IEnumerable<Domain.Entities.VenueAuditLog>> GetAuditLogsByActionAsync(Guid venueId, string action, CancellationToken cancellationToken = default)
        {
            var efAuditLogs = await _context.VenueAuditLogs
                .Where(al => al.VenueId == venueId && al.Action == action)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync(cancellationToken);

            return efAuditLogs;
        }

        public async Task<IEnumerable<Domain.Entities.VenueAuditLog>> GetAuditLogsBySubUserAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            var efAuditLogs = await _context.VenueAuditLogs
                .Where(al => al.SubUserId == subUserId)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync(cancellationToken);

            return efAuditLogs;
        }

        public async Task<IEnumerable<Domain.Entities.VenueAuditLog>> GetAuditLogsByDateRangeAsync(Guid venueId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            // Ensure proper date range
            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date", nameof(endDate));

            var efAuditLogs = await _context.VenueAuditLogs
                .Where(al => al.VenueId == venueId && 
                           al.Timestamp >= startDate && 
                           al.Timestamp <= endDate)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync(cancellationToken);

            return efAuditLogs;
        }

        public async Task<IEnumerable<Domain.Entities.VenueWorkingHours>> GetWorkingHoursAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            var efWorkingHours = await _context.VenueWorkingHours
                .Where(wh => wh.VenueId == venueId && wh.IsActive)
                .OrderBy(wh => wh.DayOfWeek)
                .ToListAsync(cancellationToken);

            return efWorkingHours;
        }

        public async Task<Domain.Entities.VenueWorkingHours?> GetWorkingHoursForDayAsync(Guid venueId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        {
            var efWorkingHours = await _context.VenueWorkingHours
                .FirstOrDefaultAsync(wh => wh.VenueId == venueId && wh.DayOfWeek == dayOfWeek && wh.IsActive, cancellationToken);

            return efWorkingHours;
        }

        public async Task<IEnumerable<Domain.Entities.VenuePricing>> GetPricingAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            var efPricing = await _context.VenuePricing
                .Where(p => p.VenueId == venueId && p.IsActive)
                .OrderBy(p => p.Type)
                .ThenBy(p => p.PlayStationModel)
                .ThenBy(p => p.RoomType)
                .ToListAsync(cancellationToken);

            return efPricing;
        }

        public async Task<IEnumerable<Domain.Entities.VenuePricing>> GetPricingByTypeAsync(Guid venueId, PricingType pricingType, CancellationToken cancellationToken = default)
        {
            var efPricing = await _context.VenuePricing
                .Where(p => p.VenueId == venueId && p.Type == pricingType && p.IsActive)
                .OrderBy(p => p.PlayStationModel)
                .ThenBy(p => p.RoomType)
                .ThenBy(p => p.TimeSlotType)
                .ToListAsync(cancellationToken);

            return efPricing;
        }

        public async Task<IEnumerable<Domain.Entities.VenueImage>> GetImagesAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            var efImages = await _context.VenueImages
                .Where(i => i.VenueId == venueId && i.IsActive)
                .OrderBy(i => i.DisplayOrder)
                .ThenBy(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

            return efImages;
        }

        public async Task<Domain.Entities.VenueImage?> GetPrimaryImageAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            var efImage = await _context.VenueImages
                .FirstOrDefaultAsync(i => i.VenueId == venueId && i.IsPrimary && i.IsActive, cancellationToken);

            return efImage;
        }

        public async Task<Domain.Entities.VenuePlayStationDetails?> GetPlayStationDetailsAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            var efPlayStationDetails = await _context.VenuePlayStationDetails
                .FirstOrDefaultAsync(psd => psd.VenueId == venueId, cancellationToken);

            return efPlayStationDetails;
        }

        public async Task CleanupExpiredSubUserSessionsAsync(CancellationToken cancellationToken = default)
        {
            var cutoffTime = DateTime.UtcNow;
            
            // Get all expired sessions that are still marked as active
            var expiredSessions = await _context.VenueSubUserSessions
                .Where(s => s.IsActive && 
                           (s.RefreshTokenExpiry <= cutoffTime || 
                            s.LastActivityAt <= cutoffTime.AddDays(-30))) // Also cleanup sessions with no activity for 30 days
                .ToListAsync(cancellationToken);

            // Mark expired sessions as inactive
            foreach (var session in expiredSessions)
            {
                session.Logout("Session expired");
            }

            // Note: SaveChanges will be called by the UnitOfWork pattern
        }

        public async Task EndAllSubUserSessionsAsync(Guid subUserId, CancellationToken cancellationToken = default)
        {
            var sessions = await _context.VenueSubUserSessions
                .Where(s => s.SubUserId == subUserId && s.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                session.Logout("Admin logout");
            }
        }

        public async Task<Dictionary<VenueType, int>> GetVenueCountsByTypeAsync(CancellationToken cancellationToken = default)
        {
            var venueCounts = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive)
                .GroupBy(v => v.VenueType)
                .Select(g => new { VenueType = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // Convert to domain enums and return as dictionary
            var result = new Dictionary<VenueType, int>();
            
            // Initialize all venue types with 0 count
            foreach (VenueType venueType in Enum.GetValues<VenueType>())
            {
                result[venueType] = 0;
            }
            
            // Populate actual counts
            foreach (var venueCount in venueCounts)
            {
                result[(VenueType)venueCount.VenueType] = venueCount.Count;
            }

            return result;
        }

        public async Task<int> GetProfileCompletionRateAsync(CancellationToken cancellationToken = default)
        {
            var totalVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive)
                .CountAsync(cancellationToken);

            if (totalVenues == 0)
                return 0;

            var completedVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && v.IsProfileComplete)
                .CountAsync(cancellationToken);

            return (int)Math.Round((double)completedVenues / totalVenues * 100, 0);
        }

        public async Task<IEnumerable<Domain.Entities.Venue>> GetMostPopularVenuesAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            if (count <= 0) count = 10;
            if (count > 100) count = 100; // Limit for performance

            // For now, order by venues with complete profiles, then by created date (most recently added)
            // TODO: When booking system is implemented, order by booking frequency
            var efVenues = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && v.IsProfileComplete)
                .Include(v => v.District)
                    .ThenInclude(d => d!.Governorate)
                .Include(v => v.Images.Where(vi => vi.IsPrimary))
                .Include(v => v.WorkingHours)
                .OrderByDescending(v => v.IsProfileComplete) // Complete profiles first
                .ThenByDescending(v => v.UpdatedAt) // Recently updated venues
                .ThenBy(v => v.Name.Name) // Alphabetical as tie-breaker
                .Take(count)
                .ToListAsync(cancellationToken);

            return efVenues;
        }

        public async Task<Dictionary<string, int>> GetVenuesByDistrictAsync(CancellationToken cancellationToken = default)
        {
            var venuesByDistrict = await _context.Venues
                .Where(v => !v.IsDeleted && v.IsActive && v.District != null)
                .GroupBy(v => v.District!.NameEn)
                .Select(g => new { DistrictName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            return venuesByDistrict.ToDictionary(x => x.DistrictName ?? "Unknown", x => x.Count);
        }

        public async Task<bool> CanCreateSubUserAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            // Check if venue exists and is active
            var venue = await _context.Venues
                .FirstOrDefaultAsync(v => v.Id == venueId && v.IsActive, cancellationToken);
            
            if (venue == null)
                return false;

            // Get current sub-user count for the venue
            var currentCount = await _context.VenueSubUsers
                .CountAsync(su => su.VenueId == venueId && su.IsActive, cancellationToken);

            // Business rule: Venues can have up to 10 sub-users (configurable)
            const int maxSubUsersPerVenue = 10;
            
            return currentCount < maxSubUsersPerVenue;
        }

        public async Task<int> GetSubUserCountAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            return await _context.VenueSubUsers
                .CountAsync(su => su.VenueId == venueId && su.IsActive, cancellationToken);
        }

        public async Task<bool> HasActiveBookingsAsync(Guid venueId, CancellationToken cancellationToken = default)
        {
            // Check if there are any reservations for this venue
            // Since the Reservation model is currently basic, we'll check if any reservations exist
            // TODO: When booking system is fully implemented, add proper date/status filtering
            var hasActiveBookings = await _context.Reservations
                .Where(r => r.VenueId == venueId)
                .AnyAsync(cancellationToken);

            return hasActiveBookings;
        }

        // AutoMapper-based mapping is now handled by extension methods in Infrastructure.Mappings
    }
}