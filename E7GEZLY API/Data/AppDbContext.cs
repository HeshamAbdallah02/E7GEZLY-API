//Data/AppDbContext.cs
using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using E7GEZLY_API.Models;
using ApplicationUser = E7GEZLY_API.Models.ApplicationUser;

namespace E7GEZLY_API.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts)
            : base(opts) { }

        // Domain entities - Clean Architecture approach
        public DbSet<Domain.Entities.UserSession> UserSessions => Set<Domain.Entities.UserSession>();
        public DbSet<Domain.Entities.Venue> Venues => Set<Domain.Entities.Venue>();
        public DbSet<Domain.Entities.Reservation> Reservations => Set<Domain.Entities.Reservation>();
        public DbSet<Domain.Entities.CustomerProfile> CustomerProfiles => Set<Domain.Entities.CustomerProfile>();
        public DbSet<Domain.Entities.ExternalLogin> ExternalLogins => Set<Domain.Entities.ExternalLogin>();
        public DbSet<Domain.Entities.VenueSubUser> VenueSubUsers => Set<Domain.Entities.VenueSubUser>();
        public DbSet<Domain.Entities.VenueSubUserSession> VenueSubUserSessions => Set<Domain.Entities.VenueSubUserSession>();
        public DbSet<Domain.Entities.VenueAuditLog> VenueAuditLogs => Set<Domain.Entities.VenueAuditLog>();
        
        // Domain entities for venue-related details
        public DbSet<Domain.Entities.VenueWorkingHours> VenueWorkingHours => Set<Domain.Entities.VenueWorkingHours>();
        public DbSet<Domain.Entities.VenuePricing> VenuePricing => Set<Domain.Entities.VenuePricing>();
        public DbSet<Domain.Entities.VenueImage> VenueImages => Set<Domain.Entities.VenueImage>();
        public DbSet<Domain.Entities.VenuePlayStationDetails> VenuePlayStationDetails => Set<Domain.Entities.VenuePlayStationDetails>();
        
        // Location entities - keeping Models for now as they're not full domain entities yet
        public DbSet<Models.Governorate> Governorates { get; set; }
        public DbSet<Models.District> Districts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply Domain entity configurations - Clean Architecture approach
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.VenueConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.VenueSubUserConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.UserConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.CustomerProfileConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.UserSessionConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.VenueSubUserSessionConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.ExternalLoginConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.VenueAuditLogConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.ReservationConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.VenueWorkingHoursConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.VenuePricingConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.VenueImageConfiguration());
            builder.ApplyConfiguration(new E7GEZLY_API.Infrastructure.Persistence.Configurations.VenuePlayStationDetailsConfiguration());

            // Keep your existing BaseSyncEntity configuration for Models entities
            var syncEntities = builder.Model.GetEntityTypes()
                .Where(e => typeof(BaseSyncEntity).IsAssignableFrom(e.ClrType) &&
                            !e.ClrType.IsAbstract);

            foreach (var entityType in syncEntities)
            {
                builder.Entity(entityType.ClrType)
                    .Property(nameof(BaseSyncEntity.CreatedAt))
                    .HasDefaultValueSql("GETUTCDATE()");

                builder.Entity(entityType.ClrType)
                    .Property(nameof(BaseSyncEntity.UpdatedAt))
                    .HasDefaultValueSql("GETUTCDATE()");
            }

            // Legacy Models configurations - keeping for incremental migration
            ConfigureApplicationUser(builder);
            ConfigureGovernorate(builder);
            ConfigureDistrict(builder);
        }

        private void ConfigureApplicationUser(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure phone number if needed
                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(50);
            });
        }


        private void ConfigureGovernorate(ModelBuilder builder)
        {
            builder.Entity<Models.Governorate>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NameEn)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.NameAr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(e => e.NameEn)
                    .HasDatabaseName("IX_Governorate_NameEn");

                entity.HasIndex(e => e.NameAr)
                    .HasDatabaseName("IX_Governorate_NameAr");

                entity.ToTable("Governorates");
            });
        }

        private void ConfigureDistrict(ModelBuilder builder)
        {
            builder.Entity<Models.District>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NameEn)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.NameAr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(e => new { e.GovernorateId, e.NameEn })
                    .HasDatabaseName("IX_District_GovernorateId_NameEn");

                entity.HasOne(d => d.Governorate)
                    .WithMany(g => g.Districts)
                    .HasForeignKey(d => d.GovernorateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable("Districts");
            });
        }







    }
}