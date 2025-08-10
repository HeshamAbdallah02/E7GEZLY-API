using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ExternalLogin domain entity
/// Maps Domain.Entities.ExternalLogin to existing ExternalLogins database table
/// </summary>
public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        // Table mapping
        builder.ToTable("ExternalLogins");
        
        // Primary key
        builder.HasKey(el => el.Id);

        // Required properties
        builder.Property(el => el.UserId)
            .IsRequired()
            .HasMaxLength(450); // Same as AspNetUser Id
            
        builder.Property(el => el.Provider)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(el => el.ProviderUserId)
            .IsRequired()
            .HasMaxLength(255);

        // Optional properties
        builder.Property(el => el.ProviderEmail)
            .HasMaxLength(500);
            
        builder.Property(el => el.ProviderDisplayName)
            .HasMaxLength(200);
            
        builder.Property(el => el.LastLoginAt);

        // Base entity properties
        builder.Property(el => el.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(el => el.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for performance
        builder.HasIndex(el => new { el.Provider, el.ProviderUserId })
            .IsUnique()
            .HasDatabaseName("IX_ExternalLogin_Provider_ProviderUserId");
            
        builder.HasIndex(el => el.UserId)
            .HasDatabaseName("IX_ExternalLogin_UserId");
            
        builder.HasIndex(el => el.Provider)
            .HasDatabaseName("IX_ExternalLogin_Provider");

        // Relationships
        builder.HasOne<Models.ApplicationUser>()
            .WithMany()
            .HasForeignKey(el => el.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}