using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for VenueWorkingHours domain entity
/// Maps Domain.Entities.VenueWorkingHours to existing VenueWorkingHours database table
/// </summary>
public class VenueWorkingHoursConfiguration : IEntityTypeConfiguration<VenueWorkingHours>
{
    public void Configure(EntityTypeBuilder<VenueWorkingHours> builder)
    {
        // Table mapping
        builder.ToTable("VenueWorkingHours");
        
        // Primary key
        builder.HasKey(wh => wh.Id);

        // Required properties
        builder.Property(wh => wh.VenueId)
            .IsRequired();
            
        builder.Property(wh => wh.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(wh => wh.OpenTime)
            .IsRequired();
            
        builder.Property(wh => wh.CloseTime)
            .IsRequired();
            
        builder.Property(wh => wh.IsClosed)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(wh => wh.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Optional time slots
        builder.Property(wh => wh.MorningStartTime);
        builder.Property(wh => wh.MorningEndTime);
        builder.Property(wh => wh.EveningStartTime);
        builder.Property(wh => wh.EveningEndTime);

        // Base entity properties
        builder.Property(wh => wh.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(wh => wh.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(wh => new { wh.VenueId, wh.DayOfWeek })
            .IsUnique();

        // Relationships
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(wh => wh.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(wh => wh.HasTimeSlots);
        
        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}