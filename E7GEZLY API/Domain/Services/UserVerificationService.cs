using E7GEZLY_API.Domain.Entities;
using System.Security.Cryptography;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Domain service implementation for user verification business logic
/// Contains complex business rules for verification processes
/// </summary>
public sealed class UserVerificationService : IUserVerificationService
{
    private static readonly Random _random = new();

    public async Task<string> GenerateEmailVerificationCodeAsync()
    {
        // Generate a 6-digit numeric code for email verification
        return _random.Next(100000, 999999).ToString();
    }

    public async Task<string> GeneratePhoneVerificationCodeAsync()
    {
        // Generate a 4-digit numeric code for phone verification (for SMS)
        return _random.Next(1000, 9999).ToString();
    }

    public async Task<string> GeneratePasswordResetCodeAsync()
    {
        // Generate a more secure 8-character alphanumeric code for password reset
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public async Task<DateTime> CalculateCodeExpiryAsync(VerificationCodeType codeType)
    {
        var baseTime = DateTime.UtcNow;
        
        return codeType switch
        {
            VerificationCodeType.EmailVerification => baseTime.AddHours(24), // 24 hours for email
            VerificationCodeType.PhoneVerification => baseTime.AddMinutes(15), // 15 minutes for SMS
            VerificationCodeType.PasswordReset => baseTime.AddMinutes(30), // 30 minutes for password reset
            _ => baseTime.AddMinutes(15) // Default to 15 minutes
        };
    }

    public async Task<bool> IsValidCodeFormatAsync(string code, VerificationCodeType codeType)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return codeType switch
        {
            VerificationCodeType.EmailVerification => IsValidEmailCode(code),
            VerificationCodeType.PhoneVerification => IsValidPhoneCode(code),
            VerificationCodeType.PasswordReset => IsValidPasswordResetCode(code),
            _ => false
        };
    }

    public async Task<bool> ValidateVerificationCodeAsync(User user, string code, VerificationCodeType codeType)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return codeType switch
        {
            VerificationCodeType.EmailVerification => ValidateEmailVerificationCode(user, code),
            VerificationCodeType.PhoneVerification => ValidatePhoneVerificationCode(user, code),
            VerificationCodeType.PasswordReset => ValidatePasswordResetCode(user, code),
            _ => false
        };
    }

    public async Task<VerificationRateLimitResult> CanRequestVerificationCodeAsync(User user, VerificationCodeType codeType)
    {
        // Define rate limiting rules
        var rateLimitRules = GetRateLimitRules(codeType);
        
        // For password reset, check the specific rate limit
        if (codeType == VerificationCodeType.PasswordReset)
        {
            if (!user.CanRequestPasswordReset(rateLimitRules.CooldownPeriod))
            {
                var waitTime = user.LastPasswordResetRequest?.Add(rateLimitRules.CooldownPeriod) - DateTime.UtcNow;
                return VerificationRateLimitResult.Deny(
                    waitTime ?? TimeSpan.Zero,
                    $"Password reset requests are limited. Please wait {rateLimitRules.CooldownPeriod.TotalMinutes} minutes between requests.");
            }
        }

        // For email and phone verification, implement similar logic based on user's last verification attempt
        // This would require tracking last verification request times in the User entity
        
        return VerificationRateLimitResult.Allow();
    }

    public async Task<UserVerificationRequirements> GetVerificationRequirementsAsync(UserType userType)
    {
        return userType switch
        {
            UserType.Venue => UserVerificationRequirements.Create(
                requireEmail: true,
                requirePhone: true,  // Venue owners need both for business communications
                allowSocial: false,  // Business accounts typically don't use social login
                codeExpiry: TimeSpan.FromMinutes(30)
            ),
            
            UserType.Customer => UserVerificationRequirements.Create(
                requireEmail: true,
                requirePhone: false, // Customers can verify just email
                allowSocial: true,   // Customers can use social login
                codeExpiry: TimeSpan.FromMinutes(15)
            ),
            
            _ => UserVerificationRequirements.Create(
                requireEmail: true,
                requirePhone: false,
                allowSocial: false,
                codeExpiry: TimeSpan.FromMinutes(15)
            )
        };
    }

    private static bool ValidateEmailVerificationCode(User user, string code)
    {
        return user.EmailVerificationCode == code && 
               user.EmailVerificationCodeExpiry > DateTime.UtcNow;
    }

    private static bool ValidatePhoneVerificationCode(User user, string code)
    {
        return user.PhoneNumberVerificationCode == code && 
               user.PhoneNumberVerificationCodeExpiry > DateTime.UtcNow;
    }

    private static bool ValidatePasswordResetCode(User user, string code)
    {
        var emailValid = user.EmailPasswordResetCode == code && 
                        user.EmailPasswordResetCodeExpiry > DateTime.UtcNow &&
                        user.EmailPasswordResetCodeUsed != true;
                        
        var phoneValid = user.PhonePasswordResetCode == code && 
                        user.PhonePasswordResetCodeExpiry > DateTime.UtcNow &&
                        user.PhonePasswordResetCodeUsed != true;

        return emailValid || phoneValid;
    }

    private static bool IsValidEmailCode(string code)
    {
        // 6-digit numeric code
        return code.Length == 6 && code.All(char.IsDigit);
    }

    private static bool IsValidPhoneCode(string code)
    {
        // 4-digit numeric code
        return code.Length == 4 && code.All(char.IsDigit);
    }

    private static bool IsValidPasswordResetCode(string code)
    {
        // 8-character alphanumeric code
        return code.Length == 8 && code.All(c => char.IsLetterOrDigit(c));
    }

    private static RateLimitRules GetRateLimitRules(VerificationCodeType codeType)
    {
        return codeType switch
        {
            VerificationCodeType.EmailVerification => new RateLimitRules
            {
                MaxRequestsPerHour = 5,
                CooldownPeriod = TimeSpan.FromMinutes(5)
            },
            VerificationCodeType.PhoneVerification => new RateLimitRules
            {
                MaxRequestsPerHour = 3,
                CooldownPeriod = TimeSpan.FromMinutes(10) // Longer cooldown for SMS
            },
            VerificationCodeType.PasswordReset => new RateLimitRules
            {
                MaxRequestsPerHour = 2,
                CooldownPeriod = TimeSpan.FromMinutes(15) // Stricter for password reset
            },
            _ => new RateLimitRules
            {
                MaxRequestsPerHour = 3,
                CooldownPeriod = TimeSpan.FromMinutes(5)
            }
        };
    }

    private sealed class RateLimitRules
    {
        public int MaxRequestsPerHour { get; init; }
        public TimeSpan CooldownPeriod { get; init; }
    }
}