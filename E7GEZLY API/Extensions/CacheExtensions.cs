using E7GEZLY_API.Configuration;
using E7GEZLY_API.Services.Cache;
using E7GEZLY_API.Services.Location;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace E7GEZLY_API.Extensions
{
    public static class CacheExtensions
    {
        public static IServiceCollection AddDistributedCaching(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind cache configuration
            services.Configure<CacheConfiguration>(
                configuration.GetSection("DistributedCache"));

            var cacheConfig = configuration
                .GetSection("DistributedCache")
                .Get<CacheConfiguration>() ?? new CacheConfiguration();

            // Add Redis connection
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configOptions = ConfigurationOptions.Parse(cacheConfig.ConnectionString);
                configOptions.AbortOnConnectFail = false;
                configOptions.ConnectRetry = 3;
                configOptions.ConnectTimeout = 5000;

                return ConnectionMultiplexer.Connect(configOptions);
            });

            // Add cache service
            services.AddSingleton<ICacheService, RedisCacheService>();

            // Add cached rate limit stores
            services.AddSingleton<AspNetCoreRateLimit.IRateLimitCounterStore, DistributedRateLimitCounterStore>();

            // Decorate existing services with caching
            DecorateServicesWithCaching(services);

            return services;
        }

        private static void DecorateServicesWithCaching(IServiceCollection services)
        {
            // temporarily commented out caching decorators for services
            /*            // Decorate LocationService with caching
                        services.Decorate<ILocationService>((inner, provider) =>
                        {
                            var cache = provider.GetRequiredService<ICacheService>();
                            var config = provider.GetRequiredService<IOptions<CacheConfiguration>>();
                            var logger = provider.GetRequiredService<ILogger<CachedLocationService>>();

                            return new CachedLocationService(inner, cache, config, logger);
                        });

                        // Decorate GeocodingService with distributed caching
                        services.Decorate<IGeocodingService>((inner, provider) =>
                        {
                            var cache = provider.GetRequiredService<ICacheService>();
                            var config = provider.GetRequiredService<IOptions<CacheConfiguration>>();
                            var logger = provider.GetRequiredService<ILogger<CachedGeocodingService>>();

                            return new CachedGeocodingService(inner, cache, config, logger);
                        });*/
        }

        public static IApplicationBuilder UseDistributedCaching(this IApplicationBuilder app)
        {
            // Warm up cache with essential data
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var locationService = scope.ServiceProvider.GetRequiredService<ILocationService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                Task.Run(async () =>
                {
                    try
                    {
                        // Pre-load governorates
                        await locationService.GetGovernoratesAsync();
                        logger.LogInformation("Location cache warmed up successfully");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to warm up location cache");
                    }
                });
            }

            return app;
        }
    }
}