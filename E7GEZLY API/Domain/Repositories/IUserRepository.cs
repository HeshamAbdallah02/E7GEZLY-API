using E7GEZLY_API.Domain.Entities;

namespace E7GEZLY_API.Domain.Repositories;

/// <summary>
/// Repository interface for User aggregate root
/// Defines data access operations for users without exposing implementation details
/// </summary>
public interface IUserRepository
{
    // Query operations
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default); // Alias for compatibility
    Task<User?> GetByExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    
    // User sessions
    Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<UserSession?> GetActiveSessionByTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetActiveSessionCountAsync(Guid userId, CancellationToken cancellationToken = default);

    // Command operations
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Session management
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task EndAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Bulk operations
    Task<IEnumerable<User>> GetMultipleAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<int> GetActiveUsersCountAsync(UserType? userType = null, CancellationToken cancellationToken = default);
    
    // Statistics and reporting
    Task<Dictionary<UserType, int>> GetUserCountsByTypeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetRecentlyRegisteredAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default);
}