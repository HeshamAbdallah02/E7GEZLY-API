using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for VenueImage domain entity
/// Maps Domain.Entities.VenueImage to existing VenueImages database table
/// </summary>
public class VenueImageConfiguration : IEntityTypeConfiguration<VenueImage>
{
    public void Configure(EntityTypeBuilder<VenueImage> builder)
    {
        // Table mapping
        builder.ToTable("VenueImages");
        
        // Primary key
        builder.HasKey(i => i.Id);

        // Required properties
        builder.Property(i => i.VenueId)
            .IsRequired();
            
        builder.Property(i => i.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(i => i.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(i => i.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Optional properties
        builder.Property(i => i.Caption)
            .HasMaxLength(200);

        // Base entity properties
        builder.Property(i => i.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(i => i.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(i => new { i.VenueId, i.DisplayOrder });

        // Relationships
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(i => i.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}