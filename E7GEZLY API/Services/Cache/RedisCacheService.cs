using E7GEZLY_API.Configuration;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace E7GEZLY_API.Services.Cache
{
    /// <summary>
    /// Redis implementation of distributed caching
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly StackExchange.Redis.IDatabase _database;
        private readonly CacheConfiguration _config;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(
            IConnectionMultiplexer redis,
            IOptions<CacheConfiguration> config,
            ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _database = redis.GetDatabase();
            _config = config.Value;
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
                // Check connection health before operation
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis connection is not available for GET key: {Key}", key);
                    return default;
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(2)); // 2-second timeout

                var value = await _database.StringGetAsync(BuildKey(key));
                if (value.IsNullOrEmpty)
                    return default;

                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Redis GET operation timed out for key: {Key}", key);
                return default;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis connection error getting cached value for key: {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
                return default;
            }
        }

        public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _database.StringGetAsync(BuildKey(key));
                return value.IsNullOrEmpty ? null : value.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached string for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check connection health before operation
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis connection is not available for SET key: {Key}", key);
                    return;
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(2)); // 2-second timeout

                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var expire = expiration ?? TimeSpan.FromMinutes(_config.DefaultExpirationMinutes);

                await _database.StringSetAsync(BuildKey(key), json, expire);
                _logger.LogDebug("Successfully cached key: {Key}", key);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Redis SET operation timed out for key: {Key}", key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis connection error setting cached value for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
            }
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var expire = expiration ?? TimeSpan.FromMinutes(_config.DefaultExpirationMinutes);
                await _database.StringSetAsync(BuildKey(key), value, expire);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cached string for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _database.KeyDeleteAsync(BuildKey(key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _database.KeyExistsAsync(BuildKey(key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence for key: {Key}", key);
                return false;
            }
        }

        public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, T?>();

            try
            {
                var redisKeys = keys.Select(k => (RedisKey)BuildKey(k)).ToArray();
                var values = await _database.StringGetAsync(redisKeys);

                var keyArray = keys.ToArray();
                for (int i = 0; i < keyArray.Length; i++)
                {
                    if (!values[i].IsNullOrEmpty)
                    {
                        result[keyArray[i]] = JsonSerializer.Deserialize<T>(values[i]!, _jsonOptions);
                    }
                    else
                    {
                        result[keyArray[i]] = default;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple cached values");
            }

            return result;
        }

        public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            try
            {
                var redisKeys = keys.Select(k => (RedisKey)BuildKey(k)).ToArray();
                await _database.KeyDeleteAsync(redisKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing multiple cached values");
            }
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: BuildKey(pattern)).ToArray();

                if (keys.Any())
                {
                    await _database.KeyDeleteAsync(keys);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing keys by pattern: {Pattern}", pattern);
            }
        }

        public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    var keys = server.Keys(pattern: BuildKey(pattern));

                    return keys.Select(k => k.ToString().Replace($"{_config.InstanceName}:", "")).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting keys by pattern: {Pattern}", pattern);
                    return Enumerable.Empty<string>();
                }
            }, cancellationToken);
        }

        public async Task TagAsync(string key, params string[] tags)
        {
            try
            {
                var tasks = tags.Select(tag =>
                    _database.SetAddAsync(BuildTagKey(tag), BuildKey(key))
                ).ToArray();

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tagging key: {Key} with tags: {Tags}", key, string.Join(", ", tags));
            }
        }

        public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            try
            {
                var tagKey = BuildTagKey(tag);
                var members = await _database.SetMembersAsync(tagKey);

                if (members.Any())
                {
                    var keys = members.Select(m => (RedisKey)m.ToString()).ToArray();
                    await _database.KeyDeleteAsync(keys);
                }

                await _database.KeyDeleteAsync(tagKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing keys by tag: {Tag}", tag);
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _database.StringIncrementAsync(BuildKey(key), value);

                if (expiration.HasValue)
                {
                    await _database.KeyExpireAsync(BuildKey(key), expiration.Value);
                }

                return result;
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
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var expire = expiration ?? TimeSpan.FromMinutes(_config.DefaultExpirationMinutes);

                return await _database.StringSetAsync(BuildKey(key), json, expire, When.NotExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value if not exists for key: {Key}", key);
                return false;
            }
        }

        private string BuildKey(string key) => $"{_config.InstanceName}:{key}";
        private string BuildTagKey(string tag) => $"{_config.InstanceName}:tags:{tag}";
    }
}