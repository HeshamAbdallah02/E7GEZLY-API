using E7GEZLY_API.Domain.Common;
using E7GEZLY_API.Domain.Events;

namespace E7GEZLY_API.Domain.Entities;

/// <summary>
/// Domain entity representing a user in the E7GEZLY system
/// Supports both venue owners and customers
/// Maintains identity and authentication-related business rules
/// </summary>
public sealed class User : AggregateRoot
{
    private readonly List<UserSession> _sessions = new();
    private readonly List<ExternalLogin> _externalLogins = new();

    private User(string email, string? phoneNumber, UserType userType) : base(Guid.NewGuid())
    {
        Email = email;
        PhoneNumber = phoneNumber;
        UserType = userType;
        IsActive = true;
        FailedLoginAttempts = 0;
        
        AddDomainEvent(new UserRegisteredEvent(
            Id.ToString(),
            email,
            phoneNumber,
            userType.ToString(),
            DateTime.UtcNow));
    }

    // Factory method for creating venue users
    public static User CreateVenueUser(string email, string? phoneNumber = null)
    {
        ValidateEmail(email);
        if (phoneNumber != null)
            ValidatePhoneNumber(phoneNumber);

        return new User(email, phoneNumber, UserType.Venue);
    }

    // Factory method for creating customer users
    public static User CreateCustomerUser(string email, string? phoneNumber = null)
    {
        ValidateEmail(email);
        if (phoneNumber != null)
            ValidatePhoneNumber(phoneNumber);

        return new User(email, phoneNumber, UserType.Customer);
    }

    // Factory method for recreating existing users from persistence
    public static User CreateExistingUser(
        Guid id,
        string email,
        string? phoneNumber,
        UserType userType,
        bool isEmailVerified,
        bool isPhoneNumberVerified,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
    {
        ValidateEmail(email);
        if (phoneNumber != null)
            ValidatePhoneNumber(phoneNumber);

        var user = new User(email, phoneNumber, userType);
        user.Id = id;
        user.IsEmailVerified = isEmailVerified;
        user.IsPhoneNumberVerified = isPhoneNumberVerified;
        user.IsActive = isActive;
        user.CreatedAt = createdAt;
        user.UpdatedAt = updatedAt;
        user.ClearDomainEvents(); // Don't replay creation events for existing users
        
        return user;
    }

    public string Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public UserType UserType { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? VenueId { get; private set; }

    // Verification properties
    public bool IsPhoneNumberVerified { get; private set; }
    public string? PhoneNumberVerificationCode { get; private set; }
    public DateTime? PhoneNumberVerificationCodeExpiry { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationCode { get; private set; }
    public DateTime? EmailVerificationCodeExpiry { get; private set; }

    // Password reset properties
    public string? PhonePasswordResetCode { get; private set; }
    public DateTime? PhonePasswordResetCodeExpiry { get; private set; }
    public bool? PhonePasswordResetCodeUsed { get; private set; }
    public string? EmailPasswordResetCode { get; private set; }
    public DateTime? EmailPasswordResetCodeExpiry { get; private set; }
    public bool? EmailPasswordResetCodeUsed { get; private set; }
    public DateTime? LastPasswordResetRequest { get; private set; }

    // Security properties
    public string? PasswordHash { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public DateTime? LastFailedLoginAt { get; private set; }
    public int AccessFailedCount { get; private set; }

    // Navigation properties
    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    // Email verification methods
    public void SetEmailVerificationCode(string code, DateTime expiry)
    {
        ValidateVerificationCode(code);
        
        if (expiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Email verification code expiry must be in the future");

        EmailVerificationCode = code;
        EmailVerificationCodeExpiry = expiry;
        MarkAsUpdated();
    }

    public void VerifyEmail(string code)
    {
        if (IsEmailVerified)
            throw new BusinessRuleViolationException("Email is already verified");

        if (string.IsNullOrEmpty(EmailVerificationCode))
            throw new BusinessRuleViolationException("No email verification code has been set");

        if (EmailVerificationCode != code)
            throw new BusinessRuleViolationException("Invalid email verification code");

        if (EmailVerificationCodeExpiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Email verification code has expired");

        IsEmailVerified = true;
        EmailVerificationCode = null;
        EmailVerificationCodeExpiry = null;
        MarkAsUpdated();

        AddDomainEvent(new UserEmailVerifiedEvent(Id.ToString(), Email, DateTime.UtcNow));
    }

    // Phone verification methods
    public void SetPhoneVerificationCode(string code, DateTime expiry)
    {
        if (string.IsNullOrEmpty(PhoneNumber))
            throw new BusinessRuleViolationException("Cannot set phone verification code when phone number is not set");

        ValidateVerificationCode(code);
        
        if (expiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Phone verification code expiry must be in the future");

        PhoneNumberVerificationCode = code;
        PhoneNumberVerificationCodeExpiry = expiry;
        MarkAsUpdated();
    }

    public void VerifyPhoneNumber(string code)
    {
        if (string.IsNullOrEmpty(PhoneNumber))
            throw new BusinessRuleViolationException("Cannot verify phone number when phone number is not set");

        if (IsPhoneNumberVerified)
            throw new BusinessRuleViolationException("Phone number is already verified");

        if (string.IsNullOrEmpty(PhoneNumberVerificationCode))
            throw new BusinessRuleViolationException("No phone verification code has been set");

        if (PhoneNumberVerificationCode != code)
            throw new BusinessRuleViolationException("Invalid phone verification code");

        if (PhoneNumberVerificationCodeExpiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Phone verification code has expired");

        IsPhoneNumberVerified = true;
        PhoneNumberVerificationCode = null;
        PhoneNumberVerificationCodeExpiry = null;
        MarkAsUpdated();

        AddDomainEvent(new UserPhoneVerifiedEvent(Id.ToString(), PhoneNumber, DateTime.UtcNow));
    }

    // Password reset methods
    public void SetEmailPasswordResetCode(string code, DateTime expiry)
    {
        ValidateVerificationCode(code);
        
        if (expiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Password reset code expiry must be in the future");

        EmailPasswordResetCode = code;
        EmailPasswordResetCodeExpiry = expiry;
        EmailPasswordResetCodeUsed = false;
        LastPasswordResetRequest = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void SetPhonePasswordResetCode(string code, DateTime expiry)
    {
        if (string.IsNullOrEmpty(PhoneNumber))
            throw new BusinessRuleViolationException("Cannot set phone password reset code when phone number is not set");

        ValidateVerificationCode(code);
        
        if (expiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Password reset code expiry must be in the future");

        PhonePasswordResetCode = code;
        PhonePasswordResetCodeExpiry = expiry;
        PhonePasswordResetCodeUsed = false;
        LastPasswordResetRequest = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public bool CanRequestPasswordReset(TimeSpan cooldownPeriod)
    {
        if (LastPasswordResetRequest == null) return true;
        return DateTime.UtcNow > LastPasswordResetRequest.Value.Add(cooldownPeriod);
    }

    public void MarkPasswordResetCodeAsUsed(bool isEmailReset)
    {
        if (isEmailReset)
        {
            EmailPasswordResetCodeUsed = true;
            EmailPasswordResetCode = null;
            EmailPasswordResetCodeExpiry = null;
        }
        else
        {
            PhonePasswordResetCodeUsed = true;
            PhonePasswordResetCode = null;
            PhonePasswordResetCodeExpiry = null;
        }

        MarkAsUpdated();
        AddDomainEvent(new UserPasswordChangedEvent(Id.ToString(), Email, true, DateTime.UtcNow));
    }

    // Password management methods
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new BusinessRuleViolationException("Password hash is required");

        PasswordHash = passwordHash;
        MarkAsUpdated();
    }

    // Security methods
    public void RecordFailedLoginAttempt()
    {
        FailedLoginAttempts++;
        AccessFailedCount++;
        LastFailedLoginAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        AccessFailedCount = 0;
        LastFailedLoginAt = null;
        MarkAsUpdated();
    }

    public void LockOut(DateTime lockoutEnd)
    {
        if (lockoutEnd <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Lockout end time must be in the future");

        LockoutEnd = lockoutEnd;
        MarkAsUpdated();

        AddDomainEvent(new UserLockedOutEvent(Id.ToString(), Email, lockoutEnd, FailedLoginAttempts));
    }

    public bool IsLockedOut()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }

    public void ClearLockout()
    {
        LockoutEnd = null;
        FailedLoginAttempts = 0;
        MarkAsUpdated();
    }

    // Venue assignment (for venue users)
    public void AssignVenue(Guid venueId)
    {
        if (UserType != UserType.Venue)
            throw new BusinessRuleViolationException("Only venue users can be assigned to venues");

        VenueId = venueId;
        MarkAsUpdated();
    }

    // Session management
    public UserSession CreateSession(string refreshToken, DateTime expiry, string? deviceName = null, 
        string? deviceType = null, string? userAgent = null, string? ipAddress = null)
    {
        var session = UserSession.Create(Id.ToString(), refreshToken, expiry, deviceName, deviceType, userAgent, ipAddress);
        _sessions.Add(session);
        MarkAsUpdated();
        return session;
    }

    public void EndSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            session.Deactivate();
            MarkAsUpdated();
        }
    }

    public void EndAllSessions()
    {
        foreach (var session in _sessions.Where(s => s.IsActive))
        {
            session.Deactivate();
        }
        MarkAsUpdated();
    }

    // External login management
    public void AddExternalLogin(string provider, string providerUserId, string? providerEmail = null, string? providerDisplayName = null)
    {
        ValidateProvider(provider);
        ValidateProviderUserId(providerUserId);

        if (_externalLogins.Any(el => el.Provider == provider))
            throw new BusinessRuleViolationException($"External login for provider {provider} already exists");

        var externalLogin = ExternalLogin.Create(Id.ToString(), provider, providerUserId, providerEmail, providerDisplayName);
        _externalLogins.Add(externalLogin);
        MarkAsUpdated();
    }

    public void UpdateExternalLogin(string provider, string? providerEmail = null, string? providerDisplayName = null)
    {
        var externalLogin = _externalLogins.FirstOrDefault(el => el.Provider == provider);
        if (externalLogin == null)
            throw new BusinessRuleViolationException($"External login for provider {provider} not found");

        externalLogin.UpdateInfo(providerEmail, providerDisplayName);
        MarkAsUpdated();
    }

    public void RemoveExternalLogin(string provider)
    {
        var externalLogin = _externalLogins.FirstOrDefault(el => el.Provider == provider);
        if (externalLogin != null)
        {
            _externalLogins.Remove(externalLogin);
            MarkAsUpdated();
        }
    }

    // Validation methods
    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleViolationException("Email is required");

        if (email.Length > 256)
            throw new BusinessRuleViolationException("Email cannot exceed 256 characters");

        // Basic email validation
        if (!email.Contains("@") || !email.Contains("."))
            throw new BusinessRuleViolationException("Email format is invalid");
    }

    private static void ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new BusinessRuleViolationException("Phone number cannot be empty");

        if (phoneNumber.Length > 20)
            throw new BusinessRuleViolationException("Phone number cannot exceed 20 characters");
    }

    private static void ValidateVerificationCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessRuleViolationException("Verification code is required");

        if (code.Length < 4 || code.Length > 10)
            throw new BusinessRuleViolationException("Verification code must be between 4 and 10 characters");
    }

    private static void ValidateProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new BusinessRuleViolationException("External login provider is required");

        if (provider.Length > 50)
            throw new BusinessRuleViolationException("Provider name cannot exceed 50 characters");

        var allowedProviders = new[] { "Facebook", "Google", "Apple" };
        if (!allowedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException($"Provider '{provider}' is not supported. Supported providers: {string.Join(", ", allowedProviders)}");
    }

    private static void ValidateProviderUserId(string providerUserId)
    {
        if (string.IsNullOrWhiteSpace(providerUserId))
            throw new BusinessRuleViolationException("Provider user ID is required");

        if (providerUserId.Length > 255)
            throw new BusinessRuleViolationException("Provider user ID cannot exceed 255 characters");
    }
}

/// <summary>
/// User types in the E7GEZLY system
/// </summary>
public enum UserType
{
    Venue = 0,
    Customer = 1
}

/// <summary>
/// Domain entity representing a user session
/// </summary>
public sealed class UserSession : BaseEntity
{
    private UserSession(string userId, string refreshToken, DateTime expiry, string? deviceName, 
        string? deviceType, string? userAgent, string? ipAddress) : base()
    {
        UserId = userId;
        RefreshToken = refreshToken;
        RefreshTokenExpiry = expiry;
        DeviceName = deviceName;
        DeviceType = deviceType;
        UserAgent = userAgent;
        IpAddress = ipAddress;
        LastActivityAt = DateTime.UtcNow;
        IsActive = true;
    }

    public static UserSession Create(string userId, string refreshToken, DateTime expiry, string? deviceName = null, 
        string? deviceType = null, string? userAgent = null, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BusinessRuleViolationException("User ID is required for session");

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new BusinessRuleViolationException("Refresh token is required for session");

        if (expiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Session expiry must be in the future");

        return new UserSession(userId, refreshToken, expiry, deviceName, deviceType, userAgent, ipAddress);
    }

    public static UserSession CreateExisting(
        Guid id,
        Guid userId,
        string deviceInfo,
        DateTime createdAt,
        DateTime expiresAt,
        bool isActive)
    {
        var session = new UserSession(userId.ToString(), "temp-token", expiresAt, deviceInfo, null, null, null);
        session.Id = id;
        session.CreatedAt = createdAt;
        session.IsActive = isActive;
        return session;
    }

    public string UserId { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime RefreshTokenExpiry { get; private set; }
    public string? DeviceName { get; private set; }
    public string? DeviceType { get; private set; }
    public string? UserAgent { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public bool IsActive { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }

    public bool IsExpired => RefreshTokenExpiry <= DateTime.UtcNow;

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void UpdateLocation(string? city, string? country)
    {
        City = city;
        Country = country;
        MarkAsUpdated();
    }

    public void UpdateRefreshToken(string refreshToken, DateTime expiry)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new BusinessRuleViolationException("Refresh token cannot be empty");

        if (expiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Token expiry must be in the future");

        RefreshToken = refreshToken;
        RefreshTokenExpiry = expiry;
        UpdateActivity();
    }

    public void UpdateDeviceInfo(string? deviceName, string? deviceType, string? userAgent, string? ipAddress)
    {
        if (!string.IsNullOrEmpty(deviceName))
            DeviceName = deviceName;
        if (!string.IsNullOrEmpty(deviceType))
            DeviceType = deviceType;
        if (!string.IsNullOrEmpty(userAgent))
            UserAgent = userAgent;
        if (!string.IsNullOrEmpty(ipAddress))
            IpAddress = ipAddress;
        
        UpdateActivity();
    }
}

/// <summary>
/// Domain entity representing an external login (social login)
/// </summary>
public sealed class ExternalLogin : BaseEntity
{
    private ExternalLogin(string userId, string provider, string providerUserId, 
        string? providerEmail, string? providerDisplayName) : base()
    {
        UserId = userId;
        Provider = provider;
        ProviderUserId = providerUserId;
        ProviderEmail = providerEmail;
        ProviderDisplayName = providerDisplayName;
    }

    public static ExternalLogin Create(string userId, string provider, string providerUserId, 
        string? providerEmail = null, string? providerDisplayName = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BusinessRuleViolationException("User ID is required for external login");

        if (string.IsNullOrWhiteSpace(provider))
            throw new BusinessRuleViolationException("Provider is required for external login");

        if (string.IsNullOrWhiteSpace(providerUserId))
            throw new BusinessRuleViolationException("Provider user ID is required for external login");

        return new ExternalLogin(userId, provider, providerUserId, providerEmail, providerDisplayName);
    }

    public string UserId { get; private set; }
    public string Provider { get; private set; }
    public string ProviderUserId { get; private set; }
    public string? ProviderEmail { get; private set; }
    public string? ProviderDisplayName { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public void UpdateInfo(string? providerEmail, string? providerDisplayName)
    {
        ProviderEmail = providerEmail;
        ProviderDisplayName = providerDisplayName;
        MarkAsUpdated();
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}