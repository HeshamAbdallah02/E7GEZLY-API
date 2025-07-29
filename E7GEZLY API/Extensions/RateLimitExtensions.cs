// Extensions/RateLimitExtensions.cs
using AspNetCoreRateLimit;
using E7GEZLY_API.Middleware;

namespace E7GEZLY_API.Extensions
{
    public static class RateLimitExtensions
    {
        public static IServiceCollection AddRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Memory cache for rate limit counters
            services.AddMemoryCache();

            // Configure IP rate limiting
            services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));

            // Add rate limit services
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            services.AddSingleton<IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();

            return services;
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            // Use IP rate limiting
            app.UseIpRateLimiting();

            // Use custom middleware for user-friendly messages
            app.UseMiddleware<CustomRateLimitMiddleware>();

            return app;
        }
    }
}