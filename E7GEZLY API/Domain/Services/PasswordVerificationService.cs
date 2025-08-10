using E7GEZLY_API.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Domain service implementation for password verification
/// Provides business logic while abstracting infrastructure concerns
/// </summary>
public class PasswordVerificationService : IPasswordVerificationService
{
    private readonly IPasswordHasher<Models.ApplicationUser> _passwordHasher;
    private readonly ILogger<PasswordVerificationService> _logger;

    // Business rules for password requirements
    private const int MIN_PASSWORD_LENGTH = 8;
    private const int MAX_PASSWORD_LENGTH = 100;
    private const int MAX_FAILED_ATTEMPTS_CUSTOMER = 5;
    private const int MAX_FAILED_ATTEMPTS_VENUE = 3;
    private const int LOCKOUT_DURATION_MINUTES = 30;

    public PasswordVerificationService(
        IPasswordHasher<Models.ApplicationUser> passwordHasher,
        ILogger<PasswordVerificationService> logger)
    {
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<PasswordVerificationResult> VerifyPasswordAsync(User user, string password)
    {
        if (user == null)
            return PasswordVerificationResult.Failure("User not found");

        if (string.IsNullOrEmpty(password))
            return PasswordVerificationResult.Failure("Password cannot be empty");

        try
        {
            // Create temporary ApplicationUser for Identity compatibility
            var identityUser = new Models.ApplicationUser { Id = user.Id.ToString(), PasswordHash = user.PasswordHash };
            
            var result = _passwordHasher.VerifyHashedPassword(identityUser, user.PasswordHash!, password);

            return result switch
            {
                Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success => PasswordVerificationResult.Success(),
                Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationResult.Success(needsRehashing: true),
                _ => PasswordVerificationResult.Failure("Invalid password")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password for user {UserId}", user.Id);
            return PasswordVerificationResult.Failure("Password verification failed");
        }
    }

    public async Task<PasswordValidationResult> ValidatePasswordStrengthAsync(string password, UserType userType)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required");
            return PasswordValidationResult.Failure(errors);
        }

        if (password.Length < MIN_PASSWORD_LENGTH)
            errors.Add($"Password must be at least {MIN_PASSWORD_LENGTH} characters long");

        if (password.Length > MAX_PASSWORD_LENGTH)
            errors.Add($"Password cannot exceed {MAX_PASSWORD_LENGTH} characters");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one digit");

        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");

        // Additional requirements for venue users
        if (userType == UserType.Venue)
        {
            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                errors.Add("Password must contain at least one special character");

            if (password.Length < 12)
                errors.Add("Venue passwords must be at least 12 characters long");
        }

        // Check for common patterns
        if (IsCommonPassword(password))
            errors.Add("Password is too common, please choose a stronger password");

        return errors.Any() ? PasswordValidationResult.Failure(errors) : PasswordValidationResult.Success();
    }

    public async Task<string> HashPasswordAsync(User user, string password)
    {
        // Create temporary ApplicationUser for Identity compatibility
        var identityUser = new Models.ApplicationUser { Id = user.Id.ToString() };
        
        return _passwordHasher.HashPassword(identityUser, password);
    }

    public async Task<AccountLockoutStatus> GetAccountLockoutStatusAsync(User user)
    {
        var maxAttempts = GetMaxFailedAttempts(user.UserType);
        
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return AccountLockoutStatus.LockedOut(user.LockoutEnd.Value, user.AccessFailedCount, maxAttempts);
        }

        return AccountLockoutStatus.NotLockedOut(user.AccessFailedCount, maxAttempts);
    }

    public async Task RecordFailedLoginAttemptAsync(User user)
    {
        user.RecordFailedLoginAttempt();
        
        var maxAttempts = GetMaxFailedAttempts(user.UserType);
        
        if (user.AccessFailedCount >= maxAttempts)
        {
            _logger.LogWarning("User {UserId} locked out after {FailedAttempts} failed attempts", 
                user.Id, user.AccessFailedCount);
        }
    }

    public async Task ResetFailedLoginAttemptsAsync(User user)
    {
        user.ResetFailedLoginAttempts();
    }

    private static int GetMaxFailedAttempts(UserType userType)
    {
        return userType switch
        {
            UserType.Customer => MAX_FAILED_ATTEMPTS_CUSTOMER,
            UserType.Venue => MAX_FAILED_ATTEMPTS_VENUE,
            _ => MAX_FAILED_ATTEMPTS_CUSTOMER
        };
    }

    private static bool IsCommonPassword(string password)
    {
        // List of common passwords to reject
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "123456789", "12345678", "12345", "1234567",
            "password123", "admin", "administrator", "qwerty", "abc123",
            "letmein", "welcome", "monkey", "dragon", "master"
        };

        return commonPasswords.Contains(password);
    }
}