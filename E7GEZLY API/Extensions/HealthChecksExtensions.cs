using E7GEZLY_API.Data;
using E7GEZLY_API.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace E7GEZLY_API.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddHealthCheckConfiguration(this IServiceCollection services)
        {
            // Register HTTP client for Nominatim health check
            services.AddHttpClient<NominatimHealthCheck>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            // Add health checks
            services.AddHealthChecks()
                .AddTypeActivatedCheck<NominatimHealthCheck>(
                    name: "nominatim",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "external", "geocoding" })
                .AddDbContextCheck<AppDbContext>(
                    name: "database",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "db", "sql" })
                .AddTypeActivatedCheck<RedisHealthCheck>(
                    name: "redis",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "cache", "redis" });

            return services;
        }

        public static WebApplication UseHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false, // Only basic liveness check
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            return app;
        }
    }
}