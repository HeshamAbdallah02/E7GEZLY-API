using E7GEZLY_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E7GEZLY_API.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for User domain entity
/// Maps Domain.Entities.User to ApplicationUsers table via ApplicationUser inheritance
/// Note: This maps to the existing ApplicationUser table structure
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table mapping - use same table as ApplicationUser
        builder.ToTable("AspNetUsers");
        
        // Primary key
        builder.HasKey(u => u.Id);

        // Required properties
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.Property(u => u.UserType)
            .HasConversion<int>()
            .IsRequired();

        // Phone number
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        // Boolean properties
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(u => u.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(u => u.IsPhoneNumberVerified)
            .IsRequired()
            .HasDefaultValue(false);

        // Optional properties
        builder.Property(u => u.VenueId);
        
        // Verification codes
        builder.Property(u => u.PhoneNumberVerificationCode)
            .HasMaxLength(10);
            
        builder.Property(u => u.PhoneNumberVerificationCodeExpiry);
        
        builder.Property(u => u.EmailVerificationCode)
            .HasMaxLength(10);
            
        builder.Property(u => u.EmailVerificationCodeExpiry);

        // Password reset properties
        builder.Property(u => u.PhonePasswordResetCode)
            .HasMaxLength(10);
            
        builder.Property(u => u.PhonePasswordResetCodeExpiry);
        
        builder.Property(u => u.PhonePasswordResetCodeUsed)
            .HasDefaultValue(false);
            
        builder.Property(u => u.EmailPasswordResetCode)
            .HasMaxLength(10);
            
        builder.Property(u => u.EmailPasswordResetCodeExpiry);
        
        builder.Property(u => u.EmailPasswordResetCodeUsed)
            .HasDefaultValue(false);
            
        builder.Property(u => u.LastPasswordResetRequest);

        // Security properties
        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500);
            
        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);
            
        builder.Property(u => u.LockoutEnd);
        
        builder.Property(u => u.LastFailedLoginAt);
        
        builder.Property(u => u.AccessFailedCount)
            .HasDefaultValue(0);

        // Base entity properties
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();
            
        builder.HasIndex(u => u.PhoneNumber);

        // Relationships
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(u => u.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore navigation collections
        builder.Ignore(u => u.Sessions);
        builder.Ignore(u => u.ExternalLogins);
        
        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}