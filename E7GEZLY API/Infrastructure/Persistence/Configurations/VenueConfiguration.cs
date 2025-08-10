using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Venue domain entity
/// Maps Domain.Entities.Venue to existing Venues database table
/// </summary>
public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        // Table mapping
        builder.ToTable("Venues");
        
        // Primary key
        builder.HasKey(v => v.Id);
        
        // Value object mappings
        builder.OwnsOne(v => v.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.Name)
                .HasColumnName("Name")
                .HasMaxLength(200)
                .IsRequired();
        });

        builder.OwnsOne(v => v.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.StreetAddress)
                .HasColumnName("StreetAddress")
                .HasMaxLength(500);
                
            addressBuilder.Property(a => a.Landmark)
                .HasColumnName("Landmark")
                .HasMaxLength(200);
                
            addressBuilder.OwnsOne(a => a.Coordinates, coordBuilder =>
            {
                coordBuilder.Property(c => c.Latitude)
                    .HasColumnName("Latitude");
                    
                coordBuilder.Property(c => c.Longitude)
                    .HasColumnName("Longitude");
            });
        });

        // Enum properties
        builder.Property(v => v.VenueType)
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(v => v.Features)
            .HasConversion<long>();

        // Simple properties
        builder.Property(v => v.DistrictSystemId)
            .HasColumnName("DistrictId");
            
        builder.Property(v => v.IsProfileComplete)
            .IsRequired();
            
        builder.Property(v => v.RequiresSubUserSetup)
            .IsRequired();
            
        builder.Property(v => v.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Contact and social properties
        builder.Property(v => v.PhoneNumber)
            .HasMaxLength(20);
            
        builder.Property(v => v.WhatsAppNumber)
            .HasMaxLength(20);
            
        builder.Property(v => v.FacebookUrl)
            .HasMaxLength(500);
            
        builder.Property(v => v.InstagramUrl)
            .HasMaxLength(500);
            
        builder.Property(v => v.Description)
            .HasMaxLength(1000);

        // Base entity properties
        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(v => v.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships - explicitly configure foreign key relationships
        builder.HasOne<Models.District>()
            .WithMany()
            .HasForeignKey(v => v.DistrictSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation properties are configured in their respective configurations
        // This avoids circular dependencies
        
        // Ignore computed properties that shouldn't be persisted
        builder.Ignore(v => v.City);
        builder.Ignore(v => v.Governorate);
        builder.Ignore(v => v.District);
        
        // Ignore collections - they're configured in their own entity configurations
        builder.Ignore(v => v.SubUsers);
        builder.Ignore(v => v.WorkingHours);
        builder.Ignore(v => v.Pricing);
        builder.Ignore(v => v.Images);
        builder.Ignore(v => v.AuditLogs);
        builder.Ignore(v => v.Reservations);
        builder.Ignore(v => v.PlayStationDetails);
        
        // Ignore domain events (handled by MediatR)
        builder.Ignore("_domainEvents");
    }
}