// Extensions/HealthCheckExtensions.cs
using E7GEZLY_API.Data;
using E7GEZLY_API.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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
                .AddTypeActivatedCheck<NominatimHealthCheck>("nominatim")
                .AddDbContextCheck<AppDbContext>("database");

            return services;
        }

        public static WebApplication UseHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            return app;
        }
    }
}