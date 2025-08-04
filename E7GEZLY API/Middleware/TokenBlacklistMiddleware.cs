// E7GEZLY API/Middleware/TokenBlacklistMiddleware.cs
using E7GEZLY_API.Services.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace E7GEZLY_API.Middleware
{
    /// <summary>
    /// Middleware to check if JWT tokens are blacklisted before processing requests
    /// </summary>
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenBlacklistMiddleware> _logger;

        public TokenBlacklistMiddleware(RequestDelegate next, ILogger<TokenBlacklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
        {
            // Only check authenticated requests with venue-operational tokens
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var tokenType = context.User.FindFirst("type")?.Value;

                // Only check venue-operational tokens (sub-user tokens)
                if (tokenType == "venue-operational")
                {
                    var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                    if (!string.IsNullOrEmpty(jti))
                    {
                        var isBlacklisted = await blacklistService.IsTokenBlacklistedAsync(jti);

                        if (isBlacklisted)
                        {
                            var subUserId = context.User.FindFirst("subUserId")?.Value;
                            _logger.LogWarning("Blocked request with blacklisted token. SubUser: {SubUserId}, JTI: {TokenId}",
                                subUserId, jti);

                            await WriteTokenRevokedResponse(context);
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Venue-operational token missing JTI claim");
                        await WriteInvalidTokenResponse(context);
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static async Task WriteTokenRevokedResponse(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "TOKEN_REVOKED",
                errorCode = "E7GEZLY_AUTH_002",
                message = "Your session has been terminated. Please log in again.",
                timestamp = DateTime.UtcNow,
                action = "LOGIN_REQUIRED"
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static async Task WriteInvalidTokenResponse(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "INVALID_TOKEN",
                errorCode = "E7GEZLY_AUTH_003",
                message = "Token is missing required claims.",
                timestamp = DateTime.UtcNow,
                action = "LOGIN_REQUIRED"
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// Extension method to register the token blacklist middleware
    /// </summary>
    public static class TokenBlacklistMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenBlacklist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenBlacklistMiddleware>();
        }
    }
}