using AspNetCoreRateLimit;
using E7GEZLY_API.Configuration;
using E7GEZLY_API.Services.Caching;
using Microsoft.Extensions.Options;

namespace E7GEZLY_API.Services.Cache
{
    /// <summary>
    /// Distributed rate limit counter store using Redis
    /// </summary>
    public class DistributedRateLimitCounterStore : IRateLimitCounterStore
    {
        private readonly ICacheService _cache;
        private readonly CacheConfiguration _config;

        public DistributedRateLimitCounterStore(
            ICacheService cache,
            IOptions<CacheConfiguration> config)
        {
            _cache = cache;
            _config = config.Value;
        }

        public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _cache.ExistsAsync($"ratelimit:{id}", cancellationToken);
        }

        public async Task<RateLimitCounter?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _cache.GetAsync<RateLimitCounter>($"ratelimit:{id}", cancellationToken);
        }

        public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync($"ratelimit:{id}", cancellationToken);
        }

        public async Task SetAsync(string id, RateLimitCounter? entry, TimeSpan? expiresAfter = null, CancellationToken cancellationToken = default)
        {
            if (entry == null) return;

            var expiration = expiresAfter ?? TimeSpan.FromMinutes(_config.Durations.RateLimitMinutes);
            await _cache.SetAsync($"ratelimit:{id}", entry, expiration, cancellationToken);
        }
    }
}