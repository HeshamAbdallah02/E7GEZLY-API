using E7GEZLY_API.Domain.Common;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Domain.Events;

namespace E7GEZLY_API.Domain.Entities;

/// <summary>
/// Domain entity representing sub-users (admins and coworkers) for venues
/// Manages authentication, authorization, and audit tracking for venue staff
/// </summary>
public sealed class VenueSubUser : BaseEntity
{
    private readonly List<VenueSubUserSession> _sessions = new();

    private VenueSubUser(Guid venueId, string username, string passwordHash, VenueSubUserRole role, 
        VenuePermissions permissions, bool isFounderAdmin = false, Guid? createdBySubUserId = null) : base()
    {
        VenueId = venueId;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        Permissions = permissions;
        IsActive = true;
        IsFounderAdmin = isFounderAdmin;
        CreatedBySubUserId = createdBySubUserId;
        FailedLoginAttempts = 0;
        MustChangePassword = false;
    }

    public static VenueSubUser CreateFounderAdmin(Guid venueId, string username, string passwordHash)
    {
        ValidateUsername(username);
        ValidatePasswordHash(passwordHash);

        return new VenueSubUser(venueId, username, passwordHash, VenueSubUserRole.Admin, 
            VenuePermissions.AdminPermissions, isFounderAdmin: true);
    }

    public static VenueSubUser Create(Guid venueId, string username, string passwordHash, 
        VenueSubUserRole role, VenuePermissions permissions, Guid createdBySubUserId)
    {
        ValidateUsername(username);
        ValidatePasswordHash(passwordHash);

        return new VenueSubUser(venueId, username, passwordHash, role, permissions, 
            isFounderAdmin: false, createdBySubUserId);
    }

    public Guid VenueId { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public VenueSubUserRole Role { get; private set; }
    public VenuePermissions Permissions { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFounderAdmin { get; private set; }
    public Guid? CreatedBySubUserId { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public DateTime? PasswordChangedAt { get; private set; }
    public bool MustChangePassword { get; private set; }
    
    // Properties to match DTO expectations
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    
    // Navigation properties for audit and relationships  
    public VenueSubUser? CreatedBy { get; private set; }

    // Navigation properties
    public IReadOnlyCollection<VenueSubUserSession> Sessions => _sessions.AsReadOnly();

    // Permission checking
    public bool HasPermission(VenuePermissions permission)
    {
        if (IsFounderAdmin) return true;
        return (Permissions & permission) == permission;
    }

    public bool HasAnyPermission(params VenuePermissions[] permissions)
    {
        if (IsFounderAdmin) return true;
        return permissions.Any(p => (Permissions & p) == p);
    }

    // Role and permissions management
    public void UpdateRole(VenueSubUserRole role)
    {
        if (IsFounderAdmin && role != VenueSubUserRole.Admin)
            throw new BusinessRuleViolationException("Founder admin must maintain admin role");

        Role = role;
        MarkAsUpdated();
    }

    public void UpdatePermissions(VenuePermissions permissions)
    {
        if (IsFounderAdmin)
            throw new BusinessRuleViolationException("Cannot modify founder admin permissions");

        Permissions = permissions;
        MarkAsUpdated();
    }

    // Password management
    public void ChangePassword(string newPasswordHash, bool isReset = false)
    {
        ValidatePasswordHash(newPasswordHash);

        PasswordHash = newPasswordHash;
        PasswordChangedAt = DateTime.UtcNow;
        MustChangePassword = false;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        MarkAsUpdated();
    }

    public void RequirePasswordChange()
    {
        if (IsFounderAdmin)
            throw new BusinessRuleViolationException("Cannot require password change for founder admin");

        MustChangePassword = true;
        MarkAsUpdated();
    }

    public void SetMustChangePassword(bool mustChange)
    {
        MustChangePassword = mustChange;
        MarkAsUpdated();
    }

    internal void SetFounderAdmin(bool isFounder)
    {
        IsFounderAdmin = isFounder;
        MarkAsUpdated();
    }

    // Authentication methods
    public void RecordSuccessfulLogin(string? deviceType = null, string? ipAddress = null)
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        MarkAsUpdated();
    }

    public void RecordFailedLoginAttempt(int maxAttempts = 5, TimeSpan lockoutDuration = default)
    {
        FailedLoginAttempts++;
        
        if (FailedLoginAttempts >= maxAttempts)
        {
            var lockoutPeriod = lockoutDuration == default ? TimeSpan.FromMinutes(15) : lockoutDuration;
            LockoutEnd = DateTime.UtcNow.Add(lockoutPeriod);
        }
        
        MarkAsUpdated();
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

    // Session management
    public VenueSubUserSession CreateSession(string refreshToken, DateTime expiry, string? deviceName = null, 
        string? deviceType = null, string? ipAddress = null, string? userAgent = null, string? accessTokenJti = null)
    {
        var session = VenueSubUserSession.Create(Id, refreshToken, expiry, deviceName, deviceType, 
            ipAddress, userAgent, accessTokenJti);
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

    // Profile management
    public void UpdateProfile(string? fullName = null, string? email = null, string? phoneNumber = null)
    {
        if (fullName != null) FullName = fullName;
        if (email != null) Email = email;
        if (phoneNumber != null) PhoneNumber = phoneNumber;
        MarkAsUpdated();
    }
    
    // Status management
    public void Activate()
    {
        IsActive = true;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        if (IsFounderAdmin)
            throw new BusinessRuleViolationException("Cannot deactivate founder admin");

        IsActive = false;
        EndAllSessions();
        MarkAsUpdated();
    }

    // Validation methods
    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new BusinessRuleViolationException("Username is required");

        if (username.Length < 3)
            throw new BusinessRuleViolationException("Username must be at least 3 characters long");

        if (username.Length > 50)
            throw new BusinessRuleViolationException("Username cannot exceed 50 characters");

        // Check for valid characters (alphanumeric, underscore, hyphen)
        if (!username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            throw new BusinessRuleViolationException("Username can only contain letters, numbers, underscores, and hyphens");
    }

    private static void ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new BusinessRuleViolationException("Password hash is required");

        if (passwordHash.Length > 500)
            throw new BusinessRuleViolationException("Password hash cannot exceed 500 characters");
    }

    public override string ToString()
    {
        return $"{Username} ({Role}) - {(IsActive ? "Active" : "Inactive")}";
    }
}

/// <summary>
/// Domain entity representing a session for venue sub-users
/// </summary>
public sealed class VenueSubUserSession : BaseEntity
{
    private VenueSubUserSession(Guid subUserId, string refreshToken, DateTime expiry, string? deviceName,
        string? deviceType, string? ipAddress, string? userAgent, string? accessTokenJti) : base()
    {
        SubUserId = subUserId;
        RefreshToken = refreshToken;
        RefreshTokenExpiry = expiry;
        DeviceName = deviceName;
        DeviceType = deviceType;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsActive = true;
        LastActivityAt = DateTime.UtcNow;
        AccessTokenJti = accessTokenJti;
    }

    public static VenueSubUserSession Create(Guid subUserId, string refreshToken, DateTime expiry, 
        string? deviceName = null, string? deviceType = null, string? ipAddress = null, 
        string? userAgent = null, string? accessTokenJti = null)
    {
        if (subUserId == Guid.Empty)
            throw new BusinessRuleViolationException("Sub-user ID is required for session");

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new BusinessRuleViolationException("Refresh token is required for session");

        if (expiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Session expiry must be in the future");

        return new VenueSubUserSession(subUserId, refreshToken, expiry, deviceName, deviceType, 
            ipAddress, userAgent, accessTokenJti);
    }

    public Guid SubUserId { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime RefreshTokenExpiry { get; private set; }
    public string? DeviceName { get; private set; }
    public string? DeviceType { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public bool IsActive { get; internal set; }
    public DateTime LastActivityAt { get; private set; }
    public string? AccessTokenJti { get; private set; }

    // Navigation properties
    public VenueSubUser? SubUser { get; private set; }

    // Logout tracking
    public DateTime? LogoutAt { get; private set; }
    public string? LogoutReason { get; private set; }

    public bool IsExpired => RefreshTokenExpiry <= DateTime.UtcNow;

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void UpdateRefreshToken(string newRefreshToken, DateTime newExpiry)
    {
        if (string.IsNullOrWhiteSpace(newRefreshToken))
            throw new BusinessRuleViolationException("Refresh token cannot be empty");

        if (newExpiry <= DateTime.UtcNow)
            throw new BusinessRuleViolationException("Token expiry must be in the future");

        RefreshToken = newRefreshToken;
        RefreshTokenExpiry = newExpiry;
        LastActivityAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void Logout(string reason)
    {
        IsActive = false;
        LogoutAt = DateTime.UtcNow;
        LogoutReason = reason;
        MarkAsUpdated();
    }

    public void SetAccessTokenJti(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti))
            throw new BusinessRuleViolationException("Access token JTI cannot be empty");

        if (jti.Length > 50)
            throw new BusinessRuleViolationException("Access token JTI cannot exceed 50 characters");

        AccessTokenJti = jti;
        MarkAsUpdated();
    }
}