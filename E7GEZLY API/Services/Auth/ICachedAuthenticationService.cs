using E7GEZLY_API.DTOs.Auth;
using E7GEZLY_API.Models;

namespace E7GEZLY_API.Services.Auth
{
    /// <summary>
    /// Interface for cached authentication service operations
    /// </summary>
    public interface ICachedAuthenticationService
    {
        /// <summary>
        /// Get user by ID with caching
        /// </summary>
        Task<ApplicationUser?> GetUserByIdAsync(string userId);

        /// <summary>
        /// Get user roles with caching
        /// </summary>
        Task<IList<string>> GetUserRolesAsync(string userId);

        /// <summary>
        /// Cache user session information
        /// </summary>
        Task CacheUserSessionAsync(string userId, UserSessionDto session);

        /// <summary>
        /// Get cached user session
        /// </summary>
        Task<UserSessionDto?> GetCachedUserSessionAsync(string userId, Guid sessionId);

        /// <summary>
        /// Invalidate user cache
        /// </summary>
        Task InvalidateUserCacheAsync(string userId);

        /// <summary>
        /// Invalidate session cache
        /// </summary>
        Task InvalidateSessionCacheAsync(string userId, Guid sessionId);

        /// <summary>
        /// Cache venue permissions for performance
        /// </summary>
        Task CacheVenuePermissionsAsync(string userId, Guid venueId, object permissions);

        /// <summary>
        /// Get cached venue permissions
        /// </summary>
        Task<T?> GetCachedVenuePermissionsAsync<T>(string userId, Guid venueId) where T : class;

        /// <summary>
        /// Invalidate venue permissions cache
        /// </summary>
        Task InvalidateVenuePermissionsCacheAsync(string userId, Guid venueId);
    }
}