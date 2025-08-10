using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;
using E7GEZLY_API.Services.Cache;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Cached wrapper for VenueAuthorizationService to improve performance
/// Implements caching strategies for frequently accessed permission checks
/// </summary>
public sealed class CachedVenueAuthorizationService : IVenueAuthorizationService
{
    private readonly VenueAuthorizationService _inner;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedVenueAuthorizationService> _logger;

    // Cache configuration
    private readonly TimeSpan _permissionCacheDuration = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _effectivePermissionsCacheDuration = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _roleValidationCacheDuration = TimeSpan.FromHours(1);

    // Cache key patterns
    private const string PERMISSION_CACHE_KEY = "venue:auth:permission:{0}:{1}:{2}"; // subUserId:permission:action
    private const string EFFECTIVE_PERMISSIONS_KEY = "venue:auth:effective:{0}"; // subUserId
    private const string ROLE_VALIDATION_KEY = "venue:auth:role-validation:{0}:{1}"; // role:permissions
    private const string SUB_USER_MANAGE_KEY = "venue:auth:manage:{0}:{1}:{2}"; // managerId:targetId:operation
    private const string VENUE_ACCESS_KEY = "venue:auth:venue-access:{0}:{1}"; // subUserId:venueId

    public CachedVenueAuthorizationService(
        VenueAuthorizationService inner,
        ICacheService cacheService,
        ILogger<CachedVenueAuthorizationService> logger)
    {
        _inner = inner;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<AuthorizationResult> CheckPermissionAsync(VenueSubUser subUser, VenuePermissions requiredPermission, string action)
    {
        var cacheKey = string.Format(PERMISSION_CACHE_KEY, subUser.Id, (int)requiredPermission, action);
        
        try
        {
            // Try to get from cache first
            var cachedResult = await _cacheService.GetAsync<AuthorizationResult>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Permission check cache hit for user {SubUserId}, permission {Permission}, action {Action}", 
                    subUser.Id, requiredPermission, action);
                return cachedResult;
            }

            // If not in cache, compute the result
            var result = await _inner.CheckPermissionAsync(subUser, requiredPermission, action);
            
            // Cache successful results for longer, failed results for shorter duration
            var cacheDuration = result.IsAuthorized ? _permissionCacheDuration : TimeSpan.FromMinutes(5);
            
            await _cacheService.SetAsync(cacheKey, result, cacheDuration);
            
            // Tag the cache entry for invalidation when user permissions change
            await _cacheService.TagAsync(cacheKey, $"user:{subUser.Id}", $"venue:{subUser.VenueId}");
            
            _logger.LogDebug("Permission check cached for user {SubUserId}, permission {Permission}, action {Action}", 
                subUser.Id, requiredPermission, action);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cached permission check for user {SubUserId}", subUser.Id);
            // Fall back to direct service call if caching fails
            return await _inner.CheckPermissionAsync(subUser, requiredPermission, action);
        }
    }

    public async Task<AuthorizationResult> CanManageSubUserAsync(VenueSubUser manager, VenueSubUser target, string operation)
    {
        var cacheKey = string.Format(SUB_USER_MANAGE_KEY, manager.Id, target.Id, operation);
        
        try
        {
            var cachedResult = await _cacheService.GetAsync<AuthorizationResult>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Sub-user management check cache hit for manager {ManagerId}, target {TargetId}, operation {Operation}", 
                    manager.Id, target.Id, operation);
                return cachedResult;
            }

            var result = await _inner.CanManageSubUserAsync(manager, target, operation);
            
            // Cache for shorter duration due to potential frequent changes in user relationships
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.TagAsync(cacheKey, $"user:{manager.Id}", $"user:{target.Id}", $"venue:{manager.VenueId}");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cached sub-user management check for manager {ManagerId}", manager.Id);
            return await _inner.CanManageSubUserAsync(manager, target, operation);
        }
    }

    public async Task<AuthorizationResult> CanAccessVenueResourceAsync(VenueSubUser subUser, Guid venueId)
    {
        var cacheKey = string.Format(VENUE_ACCESS_KEY, subUser.Id, venueId);
        
        try
        {
            var cachedResult = await _cacheService.GetAsync<AuthorizationResult>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _inner.CanAccessVenueResourceAsync(subUser, venueId);
            
            // Cache venue access checks for longer as they change infrequently
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));
            await _cacheService.TagAsync(cacheKey, $"user:{subUser.Id}", $"venue:{venueId}");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cached venue access check for user {SubUserId}", subUser.Id);
            return await _inner.CanAccessVenueResourceAsync(subUser, venueId);
        }
    }

    public async Task<VenuePermissions> GetEffectivePermissionsAsync(VenueSubUser subUser)
    {
        var cacheKey = string.Format(EFFECTIVE_PERMISSIONS_KEY, subUser.Id);
        
        try
        {
            var cachedPermissions = await _cacheService.GetAsync<VenuePermissions?>(cacheKey);
            if (cachedPermissions.HasValue)
            {
                _logger.LogDebug("Effective permissions cache hit for user {SubUserId}", subUser.Id);
                return cachedPermissions.Value;
            }

            var permissions = await _inner.GetEffectivePermissionsAsync(subUser);
            
            await _cacheService.SetAsync(cacheKey, permissions, _effectivePermissionsCacheDuration);
            await _cacheService.TagAsync(cacheKey, $"user:{subUser.Id}", $"venue:{subUser.VenueId}");
            
            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cached effective permissions check for user {SubUserId}", subUser.Id);
            return await _inner.GetEffectivePermissionsAsync(subUser);
        }
    }

    public async Task<ValidationResult> ValidatePermissionsForRoleAsync(VenueSubUserRole role, VenuePermissions permissions)
    {
        var cacheKey = string.Format(ROLE_VALIDATION_KEY, (int)role, (int)permissions);
        
        try
        {
            var cachedResult = await _cacheService.GetAsync<ValidationResult>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _inner.ValidatePermissionsForRoleAsync(role, permissions);
            
            // Role validation is static, cache for longer
            await _cacheService.SetAsync(cacheKey, result, _roleValidationCacheDuration);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cached role validation for role {Role}", role);
            return await _inner.ValidatePermissionsForRoleAsync(role, permissions);
        }
    }

    /// <summary>
    /// Invalidates all cache entries for a specific user
    /// Call this when user permissions, roles, or status changes
    /// </summary>
    public async Task InvalidateUserCacheAsync(Guid subUserId)
    {
        try
        {
            await _cacheService.RemoveByTagAsync($"user:{subUserId}");
            _logger.LogInformation("Invalidated cache for user {SubUserId}", subUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for user {SubUserId}", subUserId);
        }
    }

    /// <summary>
    /// Invalidates all cache entries for a specific venue
    /// Call this when venue settings or sub-user relationships change
    /// </summary>
    public async Task InvalidateVenueCacheAsync(Guid venueId)
    {
        try
        {
            await _cacheService.RemoveByTagAsync($"venue:{venueId}");
            _logger.LogInformation("Invalidated cache for venue {VenueId}", venueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for venue {VenueId}", venueId);
        }
    }

    /// <summary>
    /// Clears all authorization-related cache entries
    /// Use sparingly, mainly for administrative purposes
    /// </summary>
    public async Task ClearAllAuthorizationCacheAsync()
    {
        try
        {
            await _cacheService.RemoveByPatternAsync("venue:auth:*");
            _logger.LogWarning("Cleared all authorization cache entries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing authorization cache");
        }
    }
}