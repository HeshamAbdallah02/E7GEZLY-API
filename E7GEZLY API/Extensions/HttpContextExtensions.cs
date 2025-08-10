// Extensions/HttpContextExtensions.cs
using System.Security.Claims;

namespace E7GEZLY_API.Extensions
{
    public static class HttpContextExtensions
    {
        public static string? GetUserId(this HttpContext httpContext)
        {
            return httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static Guid? GetVenueId(this HttpContext httpContext)
        {
            try
            {
                return httpContext.User.GetVenueId();
            }
            catch
            {
                return null;
            }
        }

        public static string? GetCurrentRefreshToken(this HttpContext httpContext)
        {
            // Try custom header first
            var refreshTokenHeader = httpContext.Request.Headers["X-Refresh-Token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(refreshTokenHeader))
                return refreshTokenHeader;

            // Try cookie
            if (httpContext.Request.Cookies.TryGetValue("refreshToken", out var cookieToken))
                return cookieToken;

            // Try to extract from Authorization header if it contains refresh token
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.Contains("Refresh "))
            {
                return authHeader.Replace("Refresh ", "").Trim();
            }

            return null;
        }

        public static string DetectDeviceType(this HttpContext httpContext)
        {
            var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault()?.ToLower() ?? "";
            var deviceType = httpContext.Request.Headers["X-Device-Type"].FirstOrDefault();

            if (!string.IsNullOrEmpty(deviceType))
                return deviceType;

            // E7GEZLY Customer app is mobile only
            if (userAgent.Contains("e7gezly-customer") ||
                userAgent.Contains("e7gezly/customer"))
                return "Mobile";

            // E7GEZLY Venue app detection
            if (userAgent.Contains("e7gezly-venue") ||
                userAgent.Contains("e7gezly/venue"))
            {
                // Windows app for PlayStation venues
                if (userAgent.Contains("windows") || userAgent.Contains("desktop"))
                    return "Desktop";

                return "Mobile";
            }

            // Generic mobile detection
            if (userAgent.Contains("android") ||
                userAgent.Contains("iphone") ||
                userAgent.Contains("ipad") ||
                userAgent.Contains("mobile") ||
                userAgent.Contains("huawei"))
                return "Mobile";

            // Desktop detection
            if (userAgent.Contains("windows") ||
                userAgent.Contains("macintosh") ||
                userAgent.Contains("linux"))
                return "Desktop";

            return "Web";
        }

        public static string? GetClientIpAddress(this HttpContext httpContext)
        {
            // Check for forwarded IP (when behind proxy/load balancer)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take the first IP if multiple are present
                return forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }

            // Check for real IP header (some proxies use this)
            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            // Fall back to remote IP address
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        // Helper to extract device name from headers or user agent
        public static string GetDeviceName(this HttpContext httpContext)
        {
            // Try custom header first
            var deviceName = httpContext.Request.Headers["X-Device-Name"].FirstOrDefault();
            if (!string.IsNullOrEmpty(deviceName))
                return deviceName;

            // Try to parse from user agent
            var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "";

            // Common patterns
            if (userAgent.Contains("iPhone"))
                return "iPhone";
            if (userAgent.Contains("iPad"))
                return "iPad";
            if (userAgent.Contains("Android"))
            {
                // Try to extract device model
                var match = System.Text.RegularExpressions.Regex.Match(userAgent, @"Android.*?;\s*(.+?)\s*(?:Build|;|\))");
                if (match.Success && match.Groups.Count > 1)
                    return match.Groups[1].Value.Trim();
                return "Android Device";
            }
            if (userAgent.Contains("Windows"))
                return "Windows PC";
            if (userAgent.Contains("Macintosh"))
                return "Mac";

            return "Unknown Device";
        }
    }
}