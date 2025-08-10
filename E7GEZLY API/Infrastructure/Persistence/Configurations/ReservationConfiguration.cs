using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Reservation domain entity
/// Maps Domain.Entities.Reservation to existing Reservations database table
/// </summary>
public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        // Table mapping
        builder.ToTable("Reservations");
        
        // Primary key
        builder.HasKey(r => r.Id);

        // Required properties
        builder.Property(r => r.RoomName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(r => r.VenueId)
            .IsRequired();
            
        builder.Property(r => r.CustomerId)
            .IsRequired()
            .HasMaxLength(450); // Same as AspNetUser Id

        // Base entity properties
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for performance
        builder.HasIndex(r => r.VenueId)
            .HasDatabaseName("IX_Reservations_VenueId");
            
        builder.HasIndex(r => r.CustomerId)
            .HasDatabaseName("IX_Reservations_CustomerId");

        // Relationships
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(r => r.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Models.ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}