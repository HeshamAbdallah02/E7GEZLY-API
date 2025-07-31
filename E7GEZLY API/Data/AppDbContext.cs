//Data/AppDbContext.cs
using E7GEZLY_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace E7GEZLY_API.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts)
            : base(opts) { }

        public DbSet<UserSession> UserSessions => Set<UserSession>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
        public DbSet<VenueWorkingHours> VenueWorkingHours => Set<VenueWorkingHours>();
        public DbSet<VenuePricing> VenuePricing => Set<VenuePricing>();
        public DbSet<VenueImage> VenueImages => Set<VenueImage>();
        public DbSet<VenuePlayStationDetails> VenuePlayStationDetails => Set<VenuePlayStationDetails>();
        public DbSet<VenueSubUser> VenueSubUsers => Set<VenueSubUser>();
        public DbSet<VenueAuditLog> VenueAuditLogs => Set<VenueAuditLog>();
        public DbSet<VenueSubUserSession> VenueSubUserSessions => Set<VenueSubUserSession>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Keep your existing BaseSyncEntity configuration
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

            ConfigureApplicationUser(builder);
            ConfigureUserSession(builder);
            ConfigureVenue(builder);
            ConfigureCustomerProfile(builder);
            ConfigureGovernorate(builder);
            ConfigureDistrict(builder);
            ConfigureExternalLogin(builder);
            ConfigureVenueWorkingHours(builder);
            ConfigureVenuePricing(builder);
            ConfigureVenueImage(builder);
            ConfigureVenuePlayStationDetails(builder);
            ConfigureVenueSubUser(builder);
            ConfigureVenueAuditLog(builder);
            ConfigureVenueSubUserSession(builder);
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

        private void ConfigureUserSession(ModelBuilder builder)
        {
            builder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.RefreshToken)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.LastActivityAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(s => s.User)
                    .WithMany(u => u.Sessions)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.RefreshToken)
                    .IsUnique();

                entity.HasIndex(e => new { e.UserId, e.IsActive });
            });
        }
        private void ConfigureVenue(ModelBuilder builder)
        {
            builder.Entity<Venue>(entity =>
            {
                // Name configuration
                entity.Property(e => e.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                // Location - not required
                entity.Property(e => e.Latitude);

                entity.Property(e => e.Longitude);

                entity.Property(e => e.StreetAddress)
                    .HasMaxLength(500);

                entity.Property(e => e.Landmark)
                    .HasMaxLength(200);

                // Ignore computed property
                entity.Ignore(e => e.FullAddress);

                // Relationships
                entity.HasOne(v => v.District)
                    .WithMany(d => d.Venues)
                    .HasForeignKey(v => v.DistrictId)
                    .OnDelete(DeleteBehavior.Restrict);

                // One-to-one relationship with ApplicationUser
                entity.HasOne(v => v.User)
                    .WithOne(u => u.Venue)
                    .HasForeignKey<ApplicationUser>(u => u.VenueId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureCustomerProfile(ModelBuilder builder)
        {
            builder.Entity<CustomerProfile>(entity =>
            {
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.FirstName)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.LastName)
                    .HasMaxLength(50)
                    .IsRequired();

                // Ignore the computed properties
                entity.Ignore(e => e.FullName);
                entity.Ignore(e => e.FullAddress);

                // One-to-one relationship with ApplicationUser
                entity.HasOne(c => c.User)
                    .WithOne(u => u.CustomerProfile)
                    .HasForeignKey<CustomerProfile>(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureExternalLogin(ModelBuilder builder)
        {
            builder.Entity<ExternalLogin>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.Provider)
                    .IsRequired()
                    .HasMaxLength(50); // Facebook, Google, Apple

                entity.Property(e => e.ProviderUserId)
                    .IsRequired()
                    .HasMaxLength(255); // Provider's user ID

                entity.Property(e => e.ProviderEmail)
                    .HasMaxLength(500); // Email from provider (optional)

                entity.Property(e => e.ProviderDisplayName)
                    .HasMaxLength(200); // Display name from provider (optional)

                // Indexes
                entity.HasIndex(e => new { e.Provider, e.ProviderUserId })
                    .IsUnique()
                    .HasDatabaseName("IX_ExternalLogin_Provider_ProviderUserId");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_ExternalLogin_UserId");

                entity.HasIndex(e => e.Provider)
                    .HasDatabaseName("IX_ExternalLogin_Provider");

                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.ExternalLogins)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Table name (optional - EF Core will pluralize by default)
                entity.ToTable("ExternalLogins");
            });
        }

        private void ConfigureGovernorate(ModelBuilder builder)
        {
            builder.Entity<Governorate>(entity =>
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
            builder.Entity<District>(entity =>
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

        private void ConfigureVenueWorkingHours(ModelBuilder builder)
        {
            builder.Entity<VenueWorkingHours>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(wh => wh.Venue)
                    .WithMany(v => v.WorkingHours)
                    .HasForeignKey(wh => wh.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.VenueId, e.DayOfWeek })
                    .IsUnique();
            });
        }

        private void ConfigureVenuePricing(ModelBuilder builder)
        {
            builder.Entity<VenuePricing>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Price)
                    .HasPrecision(10, 2);

                entity.Property(e => e.DepositPercentage)
                    .HasPrecision(5, 2);

                entity.HasOne(p => p.Venue)
                    .WithMany(v => v.Pricing)
                    .HasForeignKey(p => p.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.VenueId, e.Type });
            });
        }

        private void ConfigureVenueImage(ModelBuilder builder)
        {
            builder.Entity<VenueImage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ImageUrl)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasOne(i => i.Venue)
                    .WithMany(v => v.Images)
                    .HasForeignKey(i => i.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.VenueId, e.DisplayOrder });
            });
        }

        private void ConfigureVenuePlayStationDetails(ModelBuilder builder)
        {
            builder.Entity<VenuePlayStationDetails>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(d => d.Venue)
                    .WithOne(v => v.PlayStationDetails)
                    .HasForeignKey<VenuePlayStationDetails>(d => d.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.VenueId)
                    .IsUnique();
            });
        }

        private void ConfigureVenueSubUser(ModelBuilder builder)
        {
            builder.Entity<VenueSubUser>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasConversion<int>();

                entity.Property(e => e.Permissions)
                    .IsRequired()
                    .HasConversion<long>();

                // Indexes
                entity.HasIndex(e => new { e.VenueId, e.Username })
                    .IsUnique()
                    .HasDatabaseName("IX_VenueSubUsers_VenueId_Username");

                entity.HasIndex(e => e.VenueId)
                    .HasDatabaseName("IX_VenueSubUsers_VenueId");

                // Relationships
                entity.HasOne(e => e.Venue)
                    .WithMany(v => v.SubUsers)
                    .HasForeignKey(e => e.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBySubUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureVenueAuditLog(ModelBuilder builder)
        {
            builder.Entity<VenueAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EntityType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EntityId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                entity.Property(e => e.Timestamp)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => new { e.VenueId, e.Timestamp })
                    .HasDatabaseName("IX_VenueAuditLogs_VenueId_Timestamp");

                entity.HasIndex(e => e.SubUserId)
                    .HasDatabaseName("IX_VenueAuditLogs_SubUserId");

                // Relationships
                entity.HasOne(e => e.Venue)
                    .WithMany(v => v.AuditLogs)
                    .HasForeignKey(e => e.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SubUser)
                    .WithMany(su => su.AuditLogs)
                    .HasForeignKey(e => e.SubUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureVenueSubUserSession(ModelBuilder builder)
        {
            builder.Entity<VenueSubUserSession>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.RefreshToken)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.DeviceName)
                    .HasMaxLength(200);

                entity.Property(e => e.DeviceType)
                    .HasMaxLength(100);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                entity.Property(e => e.RefreshTokenExpiry)
                    .IsRequired();

                entity.Property(e => e.LastActivityAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes for performance
                entity.HasIndex(e => e.SubUserId)
                    .HasDatabaseName("IX_VenueSubUserSessions_SubUserId");

                entity.HasIndex(e => e.RefreshToken)
                    .IsUnique()
                    .HasDatabaseName("IX_VenueSubUserSessions_RefreshToken");

                entity.HasIndex(e => new { e.SubUserId, e.IsActive })
                    .HasDatabaseName("IX_VenueSubUserSessions_SubUserId_IsActive");

                // Relationships
                entity.HasOne(e => e.SubUser)
                    .WithMany() // VenueSubUser doesn't need navigation property
                    .HasForeignKey(e => e.SubUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}