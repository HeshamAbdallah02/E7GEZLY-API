using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for UserSession domain entity
/// Maps Domain.Entities.UserSession to existing UserSessions database table
/// </summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        // Table mapping
        builder.ToTable("UserSessions");
        
        // Primary key
        builder.HasKey(us => us.Id);

        // Required properties
        builder.Property(us => us.UserId)
            .IsRequired()
            .HasMaxLength(450); // Same as AspNetUser Id
            
        builder.Property(us => us.RefreshToken)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(us => us.RefreshTokenExpiry)
            .IsRequired();

        // Boolean properties
        builder.Property(us => us.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Optional properties
        builder.Property(us => us.DeviceName)
            .HasMaxLength(200);
            
        builder.Property(us => us.DeviceType)
            .HasMaxLength(100);
            
        builder.Property(us => us.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(us => us.IpAddress)
            .HasMaxLength(45);
            
        builder.Property(us => us.City)
            .HasMaxLength(100);
            
        builder.Property(us => us.Country)
            .HasMaxLength(100);

        // Timestamp properties
        builder.Property(us => us.LastActivityAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Base entity properties
        builder.Property(us => us.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(us => us.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for performance
        builder.HasIndex(us => us.RefreshToken)
            .IsUnique()
            .HasDatabaseName("IX_UserSessions_RefreshToken");
            
        builder.HasIndex(us => new { us.UserId, us.IsActive })
            .HasDatabaseName("IX_UserSessions_UserId_IsActive");

        // Relationships
        builder.HasOne<Models.ApplicationUser>()
            .WithMany()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(us => us.IsExpired);
        
        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}