using AutoMapper;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Infrastructure.Mappings;
using Microsoft.EntityFrameworkCore;
using DomainUserType = E7GEZLY_API.Domain.Entities.UserType;
using ModelsUserType = E7GEZLY_API.Models.UserType;

namespace E7GEZLY_API.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for User aggregate
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        // Query operations
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appUser = await _context.Users
                .Include(u => u.Sessions)
                .Include(u => u.ExternalLogins)
                .FirstOrDefaultAsync(u => u.Id == id.ToString(), cancellationToken);

            return appUser?.ToDomainEntity();
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var appUser = await _context.Users
                .Include(u => u.Sessions)
                .Include(u => u.ExternalLogins)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            return appUser?.ToDomainEntity();
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            var appUser = await _context.Users
                .Include(u => u.Sessions)
                .Include(u => u.ExternalLogins)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);

            return appUser?.ToDomainEntity();
        }

        public async Task<User?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            // Alias method for compatibility - delegates to GetByPhoneNumberAsync
            return await GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        }

        public async Task<User?> GetByExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken = default)
        {
            var appUser = await _context.Users
                .Include(u => u.Sessions)
                .Include(u => u.ExternalLogins)
                .Where(u => u.ExternalLogins!.Any(el => el.Provider == provider && el.ProviderUserId == providerUserId))
                .FirstOrDefaultAsync(cancellationToken);

            return appUser?.ToDomainEntity();
        }

        public async Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
        }

        // User sessions
        public async Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            return session;
        }

        public async Task<UserSession?> GetActiveSessionByTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && 
                                         s.RefreshTokenExpiry > DateTime.UtcNow && 
                                         s.IsActive, cancellationToken);

            return session;
        }

        public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId.ToString() && 
                           s.RefreshTokenExpiry > DateTime.UtcNow && 
                           s.IsActive)
                .ToListAsync(cancellationToken);

            return sessions;
        }

        public async Task<int> GetActiveSessionCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserSessions
                .CountAsync(s => s.UserId == userId.ToString() && 
                                s.RefreshTokenExpiry > DateTime.UtcNow && 
                                s.IsActive, cancellationToken);
        }

        // Command operations
        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            var appUser = user.ToModel();
            var entry = await _context.Users.AddAsync(appUser, cancellationToken);
            return entry.Entity.ToDomainEntity();
        }

        public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var appUser = user.ToModel();
            _context.Users.Update(appUser);
            return appUser.ToDomainEntity();
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FindAsync(new object[] { id.ToString() }, cancellationToken);
            if (user != null)
            {
                _context.Users.Remove(user);
            }
        }

        // Session management
        public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.RefreshTokenExpiry <= DateTime.UtcNow || !s.IsActive)
                .ToListAsync(cancellationToken);

            _context.UserSessions.RemoveRange(expiredSessions);
        }

        public async Task EndAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId.ToString())
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                session.Deactivate();
            }
        }

        // Bulk operations
        public async Task<IEnumerable<User>> GetMultipleAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var stringIds = ids.Select(id => id.ToString()).ToList();
            var users = await _context.Users
                .Include(u => u.Sessions)
                .Include(u => u.ExternalLogins)
                .Where(u => stringIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            return users.ToDomainEntities();
        }

        public async Task<int> GetActiveUsersCountAsync(DomainUserType? userType = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.Where(u => u.IsActive);
            
            if (userType.HasValue)
            {
                query = query.Where(u => u.UserType == (ModelsUserType)userType.Value);
            }

            return await query.CountAsync(cancellationToken);
        }

        // Statistics and reporting
        public async Task<Dictionary<DomainUserType, int>> GetUserCountsByTypeAsync(CancellationToken cancellationToken = default)
        {
            var results = await _context.Users
                .GroupBy(u => u.UserType)
                .Select(g => new { UserType = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return results.ToDictionary(x => (DomainUserType)x.UserType, x => x.Count);
        }

        public async Task<IEnumerable<User>> GetRecentlyRegisteredAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var users = await _context.Users
                .Include(u => u.Sessions)
                .Include(u => u.ExternalLogins)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return users.ToDomainEntities();
        }

        public async Task<IEnumerable<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _context.Users
                .Include(u => u.Sessions)
                .Include(u => u.ExternalLogins)
                .Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow)
                .ToListAsync(cancellationToken);

            return users.ToDomainEntities();
        }

        // AutoMapper-based mapping is now handled by extension methods in Infrastructure.Mappings
    }
}