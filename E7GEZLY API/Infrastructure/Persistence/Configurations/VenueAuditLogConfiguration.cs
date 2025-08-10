using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for VenueAuditLog domain entity
/// Maps Domain.Entities.VenueAuditLog to existing VenueAuditLogs database table
/// </summary>
public class VenueAuditLogConfiguration : IEntityTypeConfiguration<VenueAuditLog>
{
    public void Configure(EntityTypeBuilder<VenueAuditLog> builder)
    {
        // Table mapping
        builder.ToTable("VenueAuditLogs");
        
        // Primary key
        builder.HasKey(val => val.Id);

        // Required properties
        builder.Property(val => val.VenueId)
            .IsRequired();
            
        builder.Property(val => val.Action)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(val => val.EntityType)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(val => val.EntityId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(val => val.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Optional properties
        builder.Property(val => val.SubUserId);
        
        builder.Property(val => val.OldValues)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(val => val.NewValues)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(val => val.IpAddress)
            .HasMaxLength(45);
            
        builder.Property(val => val.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(val => val.AdditionalData)
            .HasColumnType("nvarchar(max)");

        // Base entity properties
        builder.Property(val => val.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(val => val.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for performance
        builder.HasIndex(val => new { val.VenueId, val.Timestamp })
            .HasDatabaseName("IX_VenueAuditLogs_VenueId_Timestamp");
            
        builder.HasIndex(val => val.SubUserId)
            .HasDatabaseName("IX_VenueAuditLogs_SubUserId");

        // Relationships
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(val => val.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<VenueSubUser>()
            .WithMany()
            .HasForeignKey(val => val.SubUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}