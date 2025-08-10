using E7GEZLY_API.Services.Cache;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using E7GEZLY_API.Models;
using E7GEZLY_API.DTOs.Auth;

namespace E7GEZLY_API.Services.Auth
{
    /// <summary>
    /// Cached authentication service to optimize performance for authentication operations
    /// </summary>
    public class CachedAuthenticationService : ICachedAuthenticationService
    {
        private readonly ICacheService _cache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CachedAuthenticationService> _logger;

        private const int USER_CACHE_DURATION_MINUTES = 15;
        private const int PERMISSION_CACHE_DURATION_MINUTES = 30;
        private const int SESSION_CACHE_DURATION_MINUTES = 240; // 4 hours

        public CachedAuthenticationService(
            ICacheService cache,
            UserManager<ApplicationUser> userManager,
            ILogger<CachedAuthenticationService> logger)
        {
            _cache = cache;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get user by ID with caching
        /// </summary>
        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            var cacheKey = $"user:{userId}";
            
            var cachedUser = await _cache.GetAsync<ApplicationUser>(cacheKey);
            if (cachedUser != null)
            {
                _logger.LogDebug("Retrieved user {UserId} from cache", userId);
                return cachedUser;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(USER_CACHE_DURATION_MINUTES));
                _logger.LogDebug("Cached user {UserId}", userId);
            }

            return user;
        }

        /// <summary>
        /// Get user roles with caching
        /// </summary>
        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var cacheKey = $"user:roles:{userId}";
            
            var cachedRoles = await _cache.GetAsync<List<string>>(cacheKey);
            if (cachedRoles != null)
            {
                _logger.LogDebug("Retrieved roles for user {UserId} from cache", userId);
                return cachedRoles;
            }

            var user = await GetUserByIdAsync(userId);
            if (user == null) return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            var rolesList = roles.ToList();
            
            await _cache.SetAsync(cacheKey, rolesList, TimeSpan.FromMinutes(PERMISSION_CACHE_DURATION_MINUTES));
            _logger.LogDebug("Cached roles for user {UserId}", userId);

            return rolesList;
        }

        /// <summary>
        /// Cache user session information
        /// </summary>
        public async Task CacheUserSessionAsync(string userId, UserSessionDto session)
        {
            var cacheKey = $"session:{userId}:{session.Id}";
            
            await _cache.SetAsync(cacheKey, session, TimeSpan.FromMinutes(SESSION_CACHE_DURATION_MINUTES));
            _logger.LogDebug("Cached session {SessionId} for user {UserId}", session.Id, userId);
        }

        /// <summary>
        /// Get cached user session
        /// </summary>
        public async Task<UserSessionDto?> GetCachedUserSessionAsync(string userId, Guid sessionId)
        {
            var cacheKey = $"session:{userId}:{sessionId}";
            
            var cachedSession = await _cache.GetAsync<UserSessionDto>(cacheKey);
            if (cachedSession != null)
            {
                _logger.LogDebug("Retrieved session {SessionId} for user {UserId} from cache", sessionId, userId);
            }

            return cachedSession;
        }

        /// <summary>
        /// Invalidate user cache
        /// </summary>
        public async Task InvalidateUserCacheAsync(string userId)
        {
            var userCacheKey = $"user:{userId}";
            var rolesCacheKey = $"user:roles:{userId}";
            
            await _cache.RemoveAsync(userCacheKey);
            await _cache.RemoveAsync(rolesCacheKey);
            
            _logger.LogDebug("Invalidated cache for user {UserId}", userId);
        }

        /// <summary>
        /// Invalidate session cache
        /// </summary>
        public async Task InvalidateSessionCacheAsync(string userId, Guid sessionId)
        {
            var cacheKey = $"session:{userId}:{sessionId}";
            
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Invalidated session cache {SessionId} for user {UserId}", sessionId, userId);
        }

        /// <summary>
        /// Cache venue permissions for performance
        /// </summary>
        public async Task CacheVenuePermissionsAsync(string userId, Guid venueId, object permissions)
        {
            var cacheKey = $"venue:permissions:{userId}:{venueId}";
            
            await _cache.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(PERMISSION_CACHE_DURATION_MINUTES));
            _logger.LogDebug("Cached venue permissions for user {UserId} venue {VenueId}", userId, venueId);
        }

        /// <summary>
        /// Get cached venue permissions
        /// </summary>
        public async Task<T?> GetCachedVenuePermissionsAsync<T>(string userId, Guid venueId) where T : class
        {
            var cacheKey = $"venue:permissions:{userId}:{venueId}";
            
            var cachedPermissions = await _cache.GetAsync<T>(cacheKey);
            if (cachedPermissions != null)
            {
                _logger.LogDebug("Retrieved venue permissions for user {UserId} venue {VenueId} from cache", userId, venueId);
            }

            return cachedPermissions;
        }

        /// <summary>
        /// Invalidate venue permissions cache
        /// </summary>
        public async Task InvalidateVenuePermissionsCacheAsync(string userId, Guid venueId)
        {
            var cacheKey = $"venue:permissions:{userId}:{venueId}";
            
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Invalidated venue permissions cache for user {UserId} venue {VenueId}", userId, venueId);
        }
    }
}