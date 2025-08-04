// E7GEZLY API/Services/Auth/TokenBlacklistService.cs
// REPLACE your existing TokenBlacklistService with this version:

using E7GEZLY_API.Data;
using E7GEZLY_API.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Services.Auth
{
    /// <summary>
    /// Token blacklist service using ICacheService (works with both Redis and In-Memory cache)
    /// </summary>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly ICacheService _cache;
        private readonly AppDbContext _context;
        private readonly ILogger<TokenBlacklistService> _logger;

        private const string BLACKLIST_PREFIX = "blacklisted_token:";
        private const int DEFAULT_TOKEN_LIFETIME_HOURS = 4;

        public TokenBlacklistService(
            ICacheService cache,
            AppDbContext context,
            ILogger<TokenBlacklistService> logger)
        {
            _cache = cache;
            _context = context;
            _logger = logger;
        }

        public async Task BlacklistTokenAsync(string tokenId, DateTime expirationTime)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                _logger.LogWarning("Attempted to blacklist empty token ID");
                return;
            }

            try
            {
                var key = $"{BLACKLIST_PREFIX}{tokenId}";
                var ttl = expirationTime - DateTime.UtcNow;

                // Only cache if token hasn't expired yet
                if (ttl > TimeSpan.Zero)
                {
                    // Use ICacheService which handles Redis/InMemory fallback automatically
                    await _cache.SetAsync(key, true, ttl);
                    _logger.LogInformation("✅ Successfully blacklisted token {TokenId} until {ExpirationTime}",
                        tokenId, expirationTime);
                }
                else
                {
                    _logger.LogDebug("Token {TokenId} already expired, skipping blacklist", tokenId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error blacklisting token {TokenId} - LOGOUT MAY NOT WORK", tokenId);
                // Don't throw - logout should still work even if blacklisting fails
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
                return false;

            try
            {
                var key = $"{BLACKLIST_PREFIX}{tokenId}";
                var result = await _cache.GetAsync<bool?>(key);

                var isBlacklisted = result.HasValue && result.Value;

                if (isBlacklisted)
                {
                    _logger.LogDebug("🚫 Token {TokenId} is blacklisted", tokenId);
                }
                else
                {
                    _logger.LogDebug("✅ Token {TokenId} is not blacklisted", tokenId);
                }

                return isBlacklisted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking if token {TokenId} is blacklisted", tokenId);
                // If cache is down, assume token is valid to avoid blocking all requests
                _logger.LogWarning("⚠️ Cache error - assuming token {TokenId} is VALID (logout may not work)", tokenId);
                return false;
            }
        }

        public async Task BlacklistAllSubUserTokensAsync(Guid subUserId)
        {
            try
            {
                // Get all active sessions with their JTIs
                var activeSessions = await _context.VenueSubUserSessions
                    .Where(s => s.SubUserId == subUserId &&
                               s.IsActive &&
                               !string.IsNullOrEmpty(s.AccessTokenJti))
                    .Select(s => s.AccessTokenJti!)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} active sessions to blacklist for sub-user {SubUserId}",
                    activeSessions.Count, subUserId);

                if (activeSessions.Any())
                {
                    // Blacklist each token
                    var successCount = 0;
                    foreach (var jti in activeSessions)
                    {
                        try
                        {
                            await BlacklistTokenAsync(jti, DateTime.UtcNow.AddHours(DEFAULT_TOKEN_LIFETIME_HOURS));
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to blacklist token {JTI}", jti);
                        }
                    }

                    _logger.LogInformation("✅ Successfully blacklisted {SuccessCount}/{TotalCount} tokens for sub-user {SubUserId}",
                        successCount, activeSessions.Count, subUserId);

                    if (successCount == 0)
                    {
                        _logger.LogError("❌ CRITICAL: Failed to blacklist ANY tokens - logout will NOT work properly!");
                    }
                }
                else
                {
                    _logger.LogDebug("No active tokens found for sub-user {SubUserId}", subUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error blacklisting all tokens for sub-user {SubUserId}", subUserId);
                // Don't throw - logout should still work
            }
        }

        public async Task CleanupExpiredTokensAsync()
        {
            try
            {
                // For in-memory cache, we rely on TTL. For Redis, it's automatic.
                _logger.LogDebug("Token blacklist cleanup completed (automatic TTL expiration)");

                // Optional: Clean up database records if needed
                var cutoffTime = DateTime.UtcNow.AddDays(-1);
                var expiredSessions = await _context.VenueSubUserSessions
                    .Where(s => !s.IsActive && s.UpdatedAt < cutoffTime)
                    .ToListAsync();

                if (expiredSessions.Any())
                {
                    _context.VenueSubUserSessions.RemoveRange(expiredSessions);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} expired session records",
                        expiredSessions.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token blacklist cleanup");
            }
        }
    }
}