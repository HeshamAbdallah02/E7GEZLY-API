using E7GEZLY_API.Domain.Entities;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Domain service for user verification business logic
/// Encapsulates complex rules around user verification processes
/// </summary>
public interface IUserVerificationService
{
    /// <summary>
    /// Generates a verification code for email verification
    /// </summary>
    Task<string> GenerateEmailVerificationCodeAsync();
    
    /// <summary>
    /// Generates a verification code for phone verification
    /// </summary>
    Task<string> GeneratePhoneVerificationCodeAsync();
    
    /// <summary>
    /// Generates a password reset code
    /// </summary>
    Task<string> GeneratePasswordResetCodeAsync();
    
    /// <summary>
    /// Calculates the expiry time for verification codes
    /// </summary>
    Task<DateTime> CalculateCodeExpiryAsync(VerificationCodeType codeType);
    
    /// <summary>
    /// Validates if a verification code format is correct
    /// </summary>
    Task<bool> IsValidCodeFormatAsync(string code, VerificationCodeType codeType);
    
    /// <summary>
    /// Validates a verification code against user's stored code
    /// </summary>
    Task<bool> ValidateVerificationCodeAsync(User user, string code, VerificationCodeType codeType);
    
    /// <summary>
    /// Checks if a user can request a new verification code (rate limiting)
    /// </summary>
    Task<VerificationRateLimitResult> CanRequestVerificationCodeAsync(User user, VerificationCodeType codeType);
    
    /// <summary>
    /// Gets the verification requirements for a user type
    /// </summary>
    Task<UserVerificationRequirements> GetVerificationRequirementsAsync(UserType userType);
}

/// <summary>
/// Types of verification codes
/// </summary>
public enum VerificationCodeType
{
    EmailVerification,
    PhoneVerification,
    PasswordReset
}

/// <summary>
/// Result of verification rate limit check
/// </summary>
public sealed class VerificationRateLimitResult
{
    private VerificationRateLimitResult(bool canRequest, TimeSpan? waitTime, string reason)
    {
        CanRequest = canRequest;
        WaitTime = waitTime;
        Reason = reason;
    }

    public static VerificationRateLimitResult Allow() => new(true, null, string.Empty);
    public static VerificationRateLimitResult Deny(TimeSpan waitTime, string reason) => new(false, waitTime, reason);

    public bool CanRequest { get; }
    public TimeSpan? WaitTime { get; }
    public string Reason { get; }
}

/// <summary>
/// Verification requirements for different user types
/// </summary>
public sealed class UserVerificationRequirements
{
    private UserVerificationRequirements(bool requireEmailVerification, bool requirePhoneVerification, 
        bool allowSocialLogin, TimeSpan verificationCodeExpiry)
    {
        RequireEmailVerification = requireEmailVerification;
        RequirePhoneVerification = requirePhoneVerification;
        AllowSocialLogin = allowSocialLogin;
        VerificationCodeExpiry = verificationCodeExpiry;
    }

    public static UserVerificationRequirements Create(bool requireEmail, bool requirePhone, 
        bool allowSocial, TimeSpan codeExpiry)
    {
        return new UserVerificationRequirements(requireEmail, requirePhone, allowSocial, codeExpiry);
    }

    public bool RequireEmailVerification { get; }
    public bool RequirePhoneVerification { get; }
    public bool AllowSocialLogin { get; }
    public TimeSpan VerificationCodeExpiry { get; }
    
    public bool IsFullyVerified(User user)
    {
        var emailVerified = !RequireEmailVerification || user.IsEmailVerified;
        var phoneVerified = !RequirePhoneVerification || user.IsPhoneNumberVerified;
        return emailVerified && phoneVerified;
    }
}