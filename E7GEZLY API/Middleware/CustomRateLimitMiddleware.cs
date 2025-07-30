// Middleware/CustomRateLimitMiddleware.cs
using AspNetCoreRateLimit;
using E7GEZLY_API.Configuration;
using System.Net;

namespace E7GEZLY_API.Middleware
{
    /// <summary>
    /// Custom rate limit middleware for user-friendly responses
    /// </summary>
    public class CustomRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomRateLimitMiddleware> _logger;

        public CustomRateLimitMiddleware(
            RequestDelegate next,
            ILogger<CustomRateLimitMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Check if rate limit was exceeded
            if (context.Response.StatusCode == (int)HttpStatusCode.TooManyRequests)
            {
                // Get retry after header
                var retryAfter = context.Response.Headers["Retry-After"].FirstOrDefault();
                var lang = context.Request.Headers["Accept-Language"].FirstOrDefault() ?? "en";

                // Clear the default response
                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";

                // Get retry seconds
                long retrySeconds = 60; // default
                if (!string.IsNullOrEmpty(retryAfter) && long.TryParse(retryAfter, out var seconds))
                {
                    retrySeconds = seconds;
                }

                // Get user info for logging
                var userId = context.User?.Identity?.Name;
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Log the rate limit event
                LogRateLimitEvent(context, userId!, ip, retrySeconds);

                // Create user-friendly response
                var response = new
                {
                    error = "RATE_LIMIT_EXCEEDED",
                    message = E7GEZLY_API.Configuration.RateLimitConfiguration.GetFriendlyErrorMessage(lang, retrySeconds),
                    specificMessage = E7GEZLY_API.Configuration.RateLimitConfiguration.GetEndpointSpecificMessage(
                        context.Request.Path.Value ?? "", lang),
                    retryAfterSeconds = retrySeconds,
                    retryAfterTime = DateTime.UtcNow.AddSeconds(retrySeconds),
                    metadata = new
                    {
                        endpoint = context.Request.Path.Value,
                        method = context.Request.Method,
                        userAgent = context.Request.Headers["User-Agent"].FirstOrDefault()
                    }
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }

        private void LogRateLimitEvent(HttpContext context, string userId, string ip, long retrySeconds)
        {
            var eventData = new
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId ?? "anonymous",
                IpAddress = ip,
                Endpoint = context.Request.Path.Value,
                Method = context.Request.Method,
                UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                Language = context.Request.Headers["Accept-Language"].FirstOrDefault(),
                RetryAfterSeconds = retrySeconds
            };

            _logger.LogWarning("Rate limit exceeded: {@RateLimitEvent}", eventData);
        }
    }
}