using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for VenueSubUser domain entity
/// Maps Domain.Entities.VenueSubUser to existing VenueSubUsers database table
/// </summary>
public class VenueSubUserConfiguration : IEntityTypeConfiguration<VenueSubUser>
{
    public void Configure(EntityTypeBuilder<VenueSubUser> builder)
    {
        // Table mapping
        builder.ToTable("VenueSubUsers");
        
        // Primary key
        builder.HasKey(su => su.Id);

        // Required properties
        builder.Property(su => su.VenueId)
            .IsRequired();
            
        builder.Property(su => su.Username)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(su => su.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        // Enum properties
        builder.Property(su => su.Role)
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(su => su.Permissions)
            .HasConversion<long>()
            .IsRequired();

        // Boolean properties
        builder.Property(su => su.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(su => su.IsFounderAdmin)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(su => su.MustChangePassword)
            .IsRequired()
            .HasDefaultValue(false);

        // Optional properties
        builder.Property(su => su.CreatedBySubUserId);
        builder.Property(su => su.LastLoginAt);
        builder.Property(su => su.LockoutEnd);
        builder.Property(su => su.PasswordChangedAt);
        
        builder.Property(su => su.FailedLoginAttempts)
            .HasDefaultValue(0);

        // Profile properties
        builder.Property(su => su.FullName)
            .HasMaxLength(100);
            
        builder.Property(su => su.Email)
            .HasMaxLength(256);
            
        builder.Property(su => su.PhoneNumber)
            .HasMaxLength(20);

        // Base entity properties
        builder.Property(su => su.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(su => su.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for performance
        builder.HasIndex(su => new { su.VenueId, su.Username })
            .IsUnique()
            .HasDatabaseName("IX_VenueSubUsers_VenueId_Username");
            
        builder.HasIndex(su => su.VenueId)
            .HasDatabaseName("IX_VenueSubUsers_VenueId");

        // Relationships
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(su => su.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<VenueSubUser>()
            .WithMany()
            .HasForeignKey(su => su.CreatedBySubUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore navigation collections
        builder.Ignore(su => su.Sessions);
        
        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}