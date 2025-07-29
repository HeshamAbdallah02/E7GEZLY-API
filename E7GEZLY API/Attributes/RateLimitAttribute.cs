// Attributes/RateLimitAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace E7GEZLY_API.Attributes
{
    /// <summary>
    /// Custom rate limit attribute for specific endpoints
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _limit;
        private readonly int _periodInSeconds;
        private readonly string? _message;

        public RateLimitAttribute(int limit = 10, int periodInSeconds = 60, string? message = null)
        {
            _limit = limit;
            _periodInSeconds = periodInSeconds;
            _message = message;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            if (cache == null)
            {
                // If cache is not available, just continue
                await next();
                return;
            }

            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var endpoint = context.HttpContext.Request.Path;
            var key = $"rate_limit_attr_{ip}_{endpoint}";

            if (cache.TryGetValue<DateTime[]>(key, out var timestamps) && timestamps != null)
            {
                var cutoff = DateTime.UtcNow.AddSeconds(-_periodInSeconds);
                timestamps = timestamps.Where(t => t > cutoff).ToArray();

                if (timestamps.Length >= _limit)
                {
                    var oldestRequest = timestamps.Min();
                    var retryAfter = (int)(oldestRequest.AddSeconds(_periodInSeconds) - DateTime.UtcNow).TotalSeconds;

                    context.Result = new ObjectResult(new
                    {
                        error = "RATE_LIMIT_EXCEEDED",
                        message = _message ?? $"Rate limit exceeded. Limit: {_limit} requests per {_periodInSeconds} seconds",
                        retryAfterSeconds = retryAfter,
                        limit = _limit,
                        period = _periodInSeconds
                    })
                    {
                        StatusCode = 429
                    };

                    context.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
                    return;
                }

                var newTimestamps = timestamps.Append(DateTime.UtcNow).ToArray();
                cache.Set(key, newTimestamps, TimeSpan.FromSeconds(_periodInSeconds));
            }
            else
            {
                cache.Set(key, new[] { DateTime.UtcNow }, TimeSpan.FromSeconds(_periodInSeconds));
            }

            await next();
        }
    }
}