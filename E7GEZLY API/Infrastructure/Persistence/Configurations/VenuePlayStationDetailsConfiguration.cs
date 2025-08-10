using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for VenuePlayStationDetails domain entity
/// Maps Domain.Entities.VenuePlayStationDetails to existing VenuePlayStationDetails database table
/// </summary>
public class VenuePlayStationDetailsConfiguration : IEntityTypeConfiguration<VenuePlayStationDetails>
{
    public void Configure(EntityTypeBuilder<VenuePlayStationDetails> builder)
    {
        // Table mapping
        builder.ToTable("VenuePlayStationDetails");
        
        // Primary key
        builder.HasKey(d => d.Id);

        // Required properties
        builder.Property(d => d.VenueId)
            .IsRequired();
            
        builder.Property(d => d.NumberOfRooms)
            .IsRequired();
            
        builder.Property(d => d.HasPS4)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(d => d.HasPS5)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(d => d.HasVIPRooms)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(d => d.HasCafe)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(d => d.HasWiFi)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(d => d.ShowsMatches)
            .IsRequired()
            .HasDefaultValue(false);

        // Base entity properties
        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(d => d.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(d => d.VenueId)
            .IsUnique();

        // Relationships
        builder.HasOne<Venue>()
            .WithOne()
            .HasForeignKey<VenuePlayStationDetails>(d => d.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}