using E7GEZLY_API.Models;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Application.Common.Interfaces
{
    /// <summary>
    /// Application database context interface for Clean Architecture
    /// This interface provides access to the underlying EF Core DbSets for complex queries
    /// that require direct database access. For most operations, use repositories instead.
    /// Updated to use Domain entities for Clean Architecture compliance.
    /// </summary>
    public interface IApplicationDbContext
    {
        // Identity entities - still using Models for now as they're tied to ASP.NET Identity
        DbSet<ApplicationUser> Users { get; }
        
        // Domain entities - Clean Architecture approach
        DbSet<Domain.Entities.Venue> Venues { get; }
        DbSet<Domain.Entities.VenueSubUser> VenueSubUsers { get; }
        DbSet<Domain.Entities.VenueSubUserSession> VenueSubUserSessions { get; }
        DbSet<Domain.Entities.VenueAuditLog> VenueAuditLogs { get; }
        DbSet<Domain.Entities.CustomerProfile> CustomerProfiles { get; }
        DbSet<Domain.Entities.UserSession> UserSessions { get; }
        DbSet<Domain.Entities.ExternalLogin> ExternalLogins { get; }
        DbSet<Domain.Entities.Reservation> Reservations { get; }
        
        // Domain entities for venue-related details
        DbSet<Domain.Entities.VenueWorkingHours> VenueWorkingHours { get; }
        DbSet<Domain.Entities.VenuePricing> VenuePricing { get; }
        DbSet<Domain.Entities.VenueImage> VenueImages { get; }
        DbSet<Domain.Entities.VenuePlayStationDetails> VenuePlayStationDetails { get; }
        
        // Location entities - keeping Models for now as they're not full domain entities yet
        DbSet<Governorate> Governorates { get; }
        DbSet<District> Districts { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
    }
}