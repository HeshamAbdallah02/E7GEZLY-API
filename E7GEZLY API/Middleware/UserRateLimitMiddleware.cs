// Middleware/UserRateLimitMiddleware.cs
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace E7GEZLY_API.Middleware
{
    /// <summary>
    /// User-specific rate limiting for authenticated users
    /// </summary>
    public class UserRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserRateLimitMiddleware> _logger;

        // Different limits for different user types (only Customer and VenueAdmin)
        private readonly Dictionary<string, (int limit, TimeSpan period)> _userTypeLimits = new()
        {
            ["Customer"] = (100, TimeSpan.FromMinutes(1)),
            ["VenueAdmin"] = (200, TimeSpan.FromMinutes(1))
        };

        public UserRateLimitMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<UserRateLimitMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("sub")?.Value;
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value
                    ?? context.User.FindFirst("role")?.Value
                    ?? "Customer";

                if (!string.IsNullOrEmpty(userId))
                {
                    var key = $"rate_limit_{userId}_{context.Request.Path}";
                    var (limit, period) = _userTypeLimits.GetValueOrDefault(userRole, (50, TimeSpan.FromMinutes(1)));

                    // Check rate limit
                    if (_cache.TryGetValue<int>(key, out var count))
                    {
                        if (count >= limit)
                        {
                            context.Response.StatusCode = 429;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                error = "USER_RATE_LIMIT_EXCEEDED",
                                message = $"You have exceeded the rate limit of {limit} requests per {period.TotalMinutes} minute(s)",
                                userType = userRole,
                                limit,
                                period = period.TotalSeconds
                            });
                            return;
                        }

                        _cache.Set(key, count + 1, period);
                    }
                    else
                    {
                        _cache.Set(key, 1, period);
                    }
                }
            }

            await _next(context);
        }
    }
}