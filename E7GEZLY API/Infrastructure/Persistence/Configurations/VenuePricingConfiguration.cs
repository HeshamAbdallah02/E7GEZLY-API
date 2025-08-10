using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for VenuePricing domain entity
/// Maps Domain.Entities.VenuePricing to existing VenuePricing database table
/// </summary>
public class VenuePricingConfiguration : IEntityTypeConfiguration<VenuePricing>
{
    public void Configure(EntityTypeBuilder<VenuePricing> builder)
    {
        // Table mapping
        builder.ToTable("VenuePricing");
        
        // Primary key
        builder.HasKey(p => p.Id);

        // Required properties
        builder.Property(p => p.VenueId)
            .IsRequired();
            
        builder.Property(p => p.Type)
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(p => p.Price)
            .HasPrecision(10, 2)
            .IsRequired();
            
        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Optional properties
        builder.Property(p => p.Description)
            .HasMaxLength(200);
            
        builder.Property(p => p.PlayStationModel)
            .HasConversion<int?>();
            
        builder.Property(p => p.RoomType)
            .HasConversion<int?>();
            
        builder.Property(p => p.GameMode)
            .HasConversion<int?>();
            
        builder.Property(p => p.TimeSlotType)
            .HasConversion<int?>();
            
        builder.Property(p => p.DepositPercentage)
            .HasPrecision(5, 2);

        // Base entity properties
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(p => new { p.VenueId, p.Type });

        // Relationships
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(p => p.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(p => p.Name);
        builder.Ignore(p => p.PricePerHour);
        builder.Ignore(p => p.DepositAmount);
        
        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}