using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CustomerProfile domain entity
/// Maps Domain.Entities.CustomerProfile to existing CustomerProfiles database table
/// </summary>
public class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        // Table mapping
        builder.ToTable("CustomerProfiles");
        
        // Primary key
        builder.HasKey(cp => cp.Id);

        // Required properties
        builder.Property(cp => cp.UserId)
            .IsRequired()
            .HasMaxLength(450); // Same as AspNetUser Id

        // Value object mappings
        builder.OwnsOne(cp => cp.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(50)
                .IsRequired();
                
            nameBuilder.Property(n => n.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.OwnsOne(cp => cp.Address, addressBuilder =>
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

        // Optional properties
        builder.Property(cp => cp.DateOfBirth)
            .HasColumnType("date");
            
        builder.Property(cp => cp.DistrictSystemId)
            .HasColumnName("DistrictId");

        // Base entity properties
        builder.Property(cp => cp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(cp => cp.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne<Models.ApplicationUser>()
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Models.District>()
            .WithMany()
            .HasForeignKey(cp => cp.DistrictSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties
        builder.Ignore(cp => cp.Age);
        builder.Ignore(cp => cp.IsAddressComplete);
        builder.Ignore(cp => cp.District);
        
        // Ignore domain events
        builder.Ignore("_domainEvents");
        
        // Index for performance
        builder.HasIndex(cp => cp.UserId)
            .IsUnique();
    }
}