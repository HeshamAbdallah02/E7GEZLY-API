using E7GEZLY_API.Domain.Entities;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Domain service for password verification and security business logic
/// Abstracts password verification from infrastructure concerns
/// </summary>
public interface IPasswordVerificationService
{
    /// <summary>
    /// Verifies a password against the user's stored hash
    /// </summary>
    Task<PasswordVerificationResult> VerifyPasswordAsync(User user, string password);
    
    /// <summary>
    /// Checks if password meets security requirements
    /// </summary>
    Task<PasswordValidationResult> ValidatePasswordStrengthAsync(string password, UserType userType);
    
    /// <summary>
    /// Generates a secure password hash
    /// </summary>
    Task<string> HashPasswordAsync(User user, string password);
    
    /// <summary>
    /// Checks if user account is locked due to failed attempts
    /// </summary>
    Task<AccountLockoutStatus> GetAccountLockoutStatusAsync(User user);
    
    /// <summary>
    /// Records a failed login attempt
    /// </summary>
    Task RecordFailedLoginAttemptAsync(User user);
    
    /// <summary>
    /// Resets failed login attempts after successful login
    /// </summary>
    Task ResetFailedLoginAttemptsAsync(User user);
}

/// <summary>
/// Result of password verification
/// </summary>
public sealed class PasswordVerificationResult
{
    private PasswordVerificationResult(bool isValid, bool needsRehashing, string reason)
    {
        IsValid = isValid;
        NeedsRehashing = needsRehashing;
        Reason = reason;
    }
    
    public static PasswordVerificationResult Success(bool needsRehashing = false) 
        => new(true, needsRehashing, string.Empty);
    
    public static PasswordVerificationResult Failure(string reason) 
        => new(false, false, reason);
    
    public bool IsValid { get; }
    public bool NeedsRehashing { get; }
    public string Reason { get; }
}

/// <summary>
/// Result of password strength validation
/// </summary>
public sealed class PasswordValidationResult
{
    private PasswordValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }
    
    public static PasswordValidationResult Success() => new(true, new List<string>());
    public static PasswordValidationResult Failure(List<string> errors) => new(false, errors);
    public static PasswordValidationResult Failure(string error) => new(false, new List<string> { error });
    
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }
}

/// <summary>
/// Account lockout status information
/// </summary>
public sealed class AccountLockoutStatus
{
    private AccountLockoutStatus(bool isLockedOut, DateTime? lockoutEnd, int failedAttempts, int maxAttempts)
    {
        IsLockedOut = isLockedOut;
        LockoutEnd = lockoutEnd;
        FailedAttempts = failedAttempts;
        MaxAttempts = maxAttempts;
    }
    
    public static AccountLockoutStatus NotLockedOut(int failedAttempts, int maxAttempts) 
        => new(false, null, failedAttempts, maxAttempts);
    
    public static AccountLockoutStatus LockedOut(DateTime lockoutEnd, int failedAttempts, int maxAttempts) 
        => new(true, lockoutEnd, failedAttempts, maxAttempts);
    
    public bool IsLockedOut { get; }
    public DateTime? LockoutEnd { get; }
    public int FailedAttempts { get; }
    public int MaxAttempts { get; }
    
    public TimeSpan? RemainingLockoutTime => 
        LockoutEnd.HasValue ? LockoutEnd.Value - DateTime.UtcNow : null;
}