// Configuration/RateLimitConfiguration.cs
namespace E7GEZLY_API.Configuration
{
    /// <summary>
    /// Rate limiting configuration for different user scenarios
    /// </summary>
    public class RateLimitConfiguration
    {
        /// <summary>
        /// Rate limit rules for different endpoints and user types
        /// </summary>
        public static class Rules
        {
            // Anonymous users browsing venues
            public const string BrowsingRule = "browsing";

            // Authenticated customers
            public const string CustomerRule = "customer";

            // Venue owners managing their business
            public const string VenueRule = "venue";

            // Sensitive operations
            public const string SensitiveRule = "sensitive";

            // Authentication endpoints
            public const string AuthRule = "auth";
        }

        /// <summary>
        /// Get user-friendly error message based on retry after seconds
        /// </summary>
        public static string GetFriendlyErrorMessage(string lang, long retryAfterSeconds)
        {
            if (lang?.ToLower() == "ar")
            {
                if (retryAfterSeconds <= 60)
                    return $"عذراً، لقد تجاوزت الحد المسموح. يرجى المحاولة بعد {retryAfterSeconds} ثانية.";

                var minutes = retryAfterSeconds / 60;
                return $"عذراً، لقد تجاوزت الحد المسموح. يرجى المحاولة بعد {minutes} دقيقة.";
            }

            // English (default)
            if (retryAfterSeconds <= 60)
                return $"Too many requests. Please try again after {retryAfterSeconds} seconds.";

            var minutesEn = retryAfterSeconds / 60;
            return $"Too many requests. Please try again after {minutesEn} minutes.";
        }

        /// <summary>
        /// Get endpoint-specific message
        /// </summary>
        public static string? GetEndpointSpecificMessage(string endpoint, string lang)
        {
            var messages = new Dictionary<string, (string en, string ar)>
            {
                ["/api/auth/password/reset"] = (
                    "For security reasons, password reset is limited. Please wait before trying again.",
                    "لأسباب أمنية، إعادة تعيين كلمة المرور محدودة. يرجى الانتظار قبل المحاولة مرة أخرى."
                ),
                ["/api/venue/bookings"] = (
                    "Booking creation is limited to prevent spam. Please wait before creating another booking.",
                    "إنشاء الحجوزات محدود لمنع الإزعاج. يرجى الانتظار قبل إنشاء حجز آخر."
                ),
                ["/api/auth/verify"] = (
                    "Verification attempts are limited. Please check your code and try again later.",
                    "محاولات التحقق محدودة. يرجى التحقق من الرمز والمحاولة لاحقاً."
                )
            };

            if (messages.TryGetValue(endpoint, out var message))
            {
                return lang?.ToLower() == "ar" ? message.ar : message.en;
            }

            return null;
        }
    }
}