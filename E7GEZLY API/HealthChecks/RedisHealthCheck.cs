// File: E7GEZLY API/HealthChecks/RedisHealthCheck.cs

using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace E7GEZLY_API.HealthChecks
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _redis.GetDatabase();
                await database.PingAsync();

                var endpoints = _redis.GetEndPoints();
                var server = _redis.GetServer(endpoints.First());
                var info = await server.InfoAsync();

                return HealthCheckResult.Healthy($"Redis is healthy. Connected to {endpoints.First()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
            }
        }
    }
}