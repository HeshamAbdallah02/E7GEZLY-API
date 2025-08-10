// File: E7GEZLY_API.Tests/Mocks/MockCacheService.cs

using E7GEZLY_API.Services.Cache;
using System.Collections.Concurrent;

namespace E7GEZLY_API.Tests.Mocks
{
    public class MockCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, (object Value, DateTime Expiry)> _cache = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
            {
                return Task.FromResult((T?)entry.Value);
            }
            return Task.FromResult<T?>(default);
        }

        public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
            {
                return Task.FromResult(entry.Value?.ToString());
            }
            return Task.FromResult<string?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var expiry = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(60));
            _cache[key] = (value!, expiry);
            return Task.CompletedTask;
        }

        public Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var expiry = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(60));
            _cache[key] = (value, expiry);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cache.ContainsKey(key) && _cache[key].Expiry > DateTime.UtcNow);
        }

        public Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, T?>();
            foreach (var key in keys)
            {
                if (_cache.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
                {
                    result[key] = (T?)entry.Value;
                }
                else
                {
                    result[key] = default;
                }
            }
            return Task.FromResult<IDictionary<string, T?>>(result);
        }

        public Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                _cache.TryRemove(key, out _);
            }
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            var keys = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
            return Task.FromResult<IEnumerable<string>>(keys);
        }

        public Task TagAsync(string key, params string[] tags)
        {
            // Simple implementation - not needed for tests
            return Task.CompletedTask;
        }

        public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            // Simple implementation - not needed for tests
            return Task.CompletedTask;
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow && entry.Value is long currentValue)
            {
                var newValue = currentValue + value;
                _cache[key] = (newValue, entry.Expiry);
                return Task.FromResult(newValue);
            }
            else
            {
                var expiry = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(60));
                _cache[key] = (value, expiry);
                return Task.FromResult(value);
            }
        }

        public Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var expiry = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(60));
            var added = _cache.TryAdd(key, (value!, expiry));
            return Task.FromResult(added);
        }
    }
}