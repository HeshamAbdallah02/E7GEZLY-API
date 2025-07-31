// E7GEZLY API/Services/Cache/InMemoryCacheService.cs
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace E7GEZLY_API.Services.Cache
{
    /// <summary>
    /// Simple in-memory cache service implementation (fallback for Redis)
    /// </summary>
    public class InMemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, HashSet<string>> _tags;
        private readonly ILogger<InMemoryCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public InMemoryCacheService(IMemoryCache memoryCache, ILogger<InMemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _tags = new ConcurrentDictionary<string, HashSet<string>>();
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = _memoryCache.Get<T>(key);
                _logger.LogDebug("Cache {Status} for key: {Key}", value != null ? "HIT" : "MISS", key);
                return await Task.FromResult(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache key: {Key}", key);
                return default;
            }
        }

        public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = _memoryCache.Get<string>(key);
                _logger.LogDebug("Cache {Status} for string key: {Key}", value != null ? "HIT" : "MISS", key);
                return await Task.FromResult(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache string key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60); // Default 1 hour
                }

                _memoryCache.Set(key, value, options);
                _logger.LogDebug("Cached key: {Key} with expiration: {Expiration}", key, expiration);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {Key}", key);
            }
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60); // Default 1 hour
                }

                _memoryCache.Set(key, value, options);
                _logger.LogDebug("Cached string key: {Key} with expiration: {Expiration}", key, expiration);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache string key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                _memoryCache.Remove(key);
                _logger.LogDebug("Removed cache key: {Key}", key);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var exists = _memoryCache.TryGetValue(key, out _);
                return await Task.FromResult(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
                return false;
            }
        }

        public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, T?>();

            foreach (var key in keys)
            {
                result[key] = await GetAsync<T>(key, cancellationToken);
            }

            return result;
        }

        public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                await RemoveAsync(key, cancellationToken);
            }
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple pattern matching for in-memory cache
                // Note: This is a basic implementation - production would need more sophisticated pattern matching
                _logger.LogWarning("Pattern-based removal is simplified for in-memory cache: {Pattern}", pattern);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing by pattern: {Pattern}", pattern);
            }
        }

        public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple implementation - return empty for now
                _logger.LogWarning("Pattern-based key retrieval is not fully supported for in-memory cache: {Pattern}", pattern);
                return await Task.FromResult(Enumerable.Empty<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keys by pattern: {Pattern}", pattern);
                return Enumerable.Empty<string>();
            }
        }

        public async Task TagAsync(string key, params string[] tags)
        {
            try
            {
                foreach (var tag in tags)
                {
                    _tags.AddOrUpdate(tag,
                        new HashSet<string> { key },
                        (existingTag, existingKeys) =>
                        {
                            lock (existingKeys)
                            {
                                existingKeys.Add(key);
                                return existingKeys;
                            }
                        });
                }

                _logger.LogDebug("Tagged key {Key} with tags {Tags}", key, string.Join(", ", tags));
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tagging key {Key} with tags {Tags}", key, string.Join(", ", tags));
            }
        }

        public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_tags.TryGetValue(tag, out var keys))
                {
                    var keysToRemove = keys.ToList(); // Create a copy to avoid modification during enumeration
                    foreach (var key in keysToRemove)
                    {
                        await RemoveAsync(key, cancellationToken);
                    }
                    _tags.TryRemove(tag, out _);
                    _logger.LogDebug("Removed {Count} keys with tag {Tag}", keysToRemove.Count, tag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing by tag: {Tag}", tag);
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var current = await GetAsync<long?>(key, cancellationToken) ?? 0;
                var newValue = current + value;
                await SetAsync(key, newValue, expiration, cancellationToken);
                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing key: {Key}", key);
                return 0;
            }
        }

        public async Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!await ExistsAsync(key, cancellationToken))
                {
                    await SetAsync(key, value, expiration, cancellationToken);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting if not exists for key: {Key}", key);
                return false;
            }
        }
    }
}