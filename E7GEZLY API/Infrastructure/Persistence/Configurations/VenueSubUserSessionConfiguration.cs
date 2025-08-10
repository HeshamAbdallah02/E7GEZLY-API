using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for VenueSubUserSession domain entity
/// Maps Domain.Entities.VenueSubUserSession to existing VenueSubUserSessions database table
/// </summary>
public class VenueSubUserSessionConfiguration : IEntityTypeConfiguration<VenueSubUserSession>
{
    public void Configure(EntityTypeBuilder<VenueSubUserSession> builder)
    {
        // Table mapping
        builder.ToTable("VenueSubUserSessions");
        
        // Primary key
        builder.HasKey(sus => sus.Id);

        // Required properties
        builder.Property(sus => sus.SubUserId)
            .IsRequired();
            
        builder.Property(sus => sus.RefreshToken)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(sus => sus.RefreshTokenExpiry)
            .IsRequired();

        // Boolean properties
        builder.Property(sus => sus.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Optional properties
        builder.Property(sus => sus.DeviceName)
            .HasMaxLength(200);
            
        builder.Property(sus => sus.DeviceType)
            .HasMaxLength(100);
            
        builder.Property(sus => sus.IpAddress)
            .HasMaxLength(45);
            
        builder.Property(sus => sus.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(sus => sus.AccessTokenJti)
            .HasMaxLength(50);

        // Timestamp properties
        builder.Property(sus => sus.LastActivityAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Base entity properties
        builder.Property(sus => sus.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(sus => sus.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for performance
        builder.HasIndex(sus => sus.SubUserId)
            .HasDatabaseName("IX_VenueSubUserSessions_SubUserId");
            
        builder.HasIndex(sus => sus.RefreshToken)
            .IsUnique()
            .HasDatabaseName("IX_VenueSubUserSessions_RefreshToken");
            
        builder.HasIndex(sus => new { sus.SubUserId, sus.IsActive })
            .HasDatabaseName("IX_VenueSubUserSessions_SubUserId_IsActive");

        // Relationships
        builder.HasOne<VenueSubUser>()
            .WithMany()
            .HasForeignKey(sus => sus.SubUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(sus => sus.IsExpired);
        
        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}