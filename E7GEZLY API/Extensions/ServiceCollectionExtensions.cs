// E7GEZLY API/Extensions/ServiceCollectionExtensions.cs
using E7GEZLY_API.Configuration;
using E7GEZLY_API.Converters;
using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Communication;
using E7GEZLY_API.Services.Location;
using E7GEZLY_API.Services.VenueManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using E7GEZLY_API.Services.Cache;
using Microsoft.Extensions.Options;

namespace E7GEZLY_API.Extensions
{
    /// <summary>
    /// Service collection extensions for E7GEZLY API
    /// 
    /// NOTE: Clean Architecture Integration
    /// ===================================
    /// The E7GEZLY API has been transformed to use Clean Architecture with the following layers:
    /// 
    /// 1. Domain Layer: E7GEZLY_API.Domain - Contains business entities, value objects, and domain services
    /// 2. Application Layer: E7GEZLY_API.Application - Contains use cases, commands, queries, and handlers
    /// 3. Infrastructure Layer: E7GEZLY_API.Infrastructure - Contains repository implementations
    /// 4. API Layer: Controllers use MediatR to communicate with Application layer
    /// 
    /// Service Registration Order (IMPORTANT):
    /// 1. Infrastructure services (AddInfrastructure) - Database, repositories
    /// 2. Application services (AddApplication) - MediatR, validators, behaviors
    /// 3. Legacy application services (AddApplicationServices) - Existing services for compatibility
    /// 4. All other services (Identity, caching, etc.)
    /// 
    /// For new features, use the Clean Architecture pattern by creating commands/queries in the Application layer.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Legacy services - these should eventually be migrated to Clean Architecture patterns
            // For now, keeping them for backwards compatibility with existing functionality
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IVerificationService, VerificationService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IProfileService, ProfileService>();

            // Add memory cache for IMemoryCache interface
            services.AddMemoryCache();

            // Register HTTP client for Nominatim geocoding service
            services.AddHttpClient<NominatimGeocodingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                // Add User-Agent header to comply with Nominatim usage policy
                client.DefaultRequestHeaders.Add("User-Agent", "E7GEZLY-API/1.0 (Egypt Venue Booking Platform)");
            });

            // Register geocoding services
            services.AddScoped<NominatimGeocodingService>();
            services.AddScoped<IGeocodingService, NominatimGeocodingService>();

            // Add HTTP context accessor for accessing user context in Application layer
            services.AddHttpContextAccessor();

            return services;
        }

        public static IServiceCollection AddIdentityServices(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }

        // NEW: Add this method for venue sub-user services
        public static IServiceCollection AddVenueSubUserServices(this IServiceCollection services)
        {
            // Register password hasher for VenueSubUser
            services.AddScoped<IPasswordHasher<VenueSubUser>, PasswordHasher<VenueSubUser>>();

            // Register venue sub-user services
            services.AddScoped<IVenueSubUserService, VenueSubUserService>();
            services.AddScoped<IVenueAuditService, VenueAuditService>();

            // Register venue sub-user token invalidation after logout
            services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

            return services;
        }

        public static IServiceCollection AddControllersWithOptions(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter());
                });

            return services;
        }

        public static IServiceCollection AddCommunicationServices(this IServiceCollection services,
            IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                services.AddScoped<IEmailService, MockEmailService>();
                services.AddScoped<ISmsService, MockSmsService>();
            }
            else
            {
                services.AddScoped<IEmailService, SendGridEmailService>();
                services.AddScoped<ISmsService, MockSmsService>(); // Replace with real SMS service
            }

            return services;
        }

        public static IServiceCollection AddMinimalCacheServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add basic cache configuration
            services.Configure<CacheConfiguration>(configuration.GetSection("DistributedCache"));

            var cacheConfig = configuration.GetSection("DistributedCache").Get<CacheConfiguration>()
                             ?? new CacheConfiguration();

            // Determine if we should attempt Redis connection
            var shouldUseRedis = ShouldAttemptRedisConnection(cacheConfig, configuration);

            if (shouldUseRedis)
            {
                try
                {
                    // Add Redis with resilient configuration
                    services.AddSingleton<IConnectionMultiplexer>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<Program>>();

                        try
                        {
                            var configOptions = ConfigurationOptions.Parse(cacheConfig.ConnectionString);

                            // Resilient Redis configuration
                            configOptions.AbortOnConnectFail = false;
                            configOptions.ConnectRetry = 2;
                            configOptions.ConnectTimeout = 2000; // 2 seconds
                            configOptions.SyncTimeout = 1000;    // 1 second
                            configOptions.AsyncTimeout = 2000;   // 2 seconds
                            configOptions.CommandMap = CommandMap.Create(new HashSet<string> { "INFO" }, available: false);
                            configOptions.KeepAlive = 60;

                            var connection = ConnectionMultiplexer.Connect(configOptions);

                            // Test the connection immediately
                            var database = connection.GetDatabase();
                            database.Ping();

                            logger.LogInformation("✅ Redis connection established successfully");
                            return connection;
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "❌ Failed to connect to Redis, will use in-memory cache");
                            throw; // Let the outer catch handle the fallback
                        }
                    });

                    // Register Redis cache service
                    services.AddSingleton<ICacheService>(sp =>
                    {
                        try
                        {
                            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                            var config = sp.GetRequiredService<IOptions<CacheConfiguration>>();
                            var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();

                            return new RedisCacheService(redis, config, logger);
                        }
                        catch
                        {
                            // Fallback to in-memory if Redis service creation fails
                            var memoryCache = sp.GetRequiredService<IMemoryCache>();
                            var logger = sp.GetRequiredService<ILogger<InMemoryCacheService>>();
                            var fallbackLogger = sp.GetRequiredService<ILogger<Program>>();

                            fallbackLogger.LogWarning("🔄 Falling back to in-memory cache due to Redis service creation failure");
                            return new InMemoryCacheService(memoryCache, logger);
                        }
                    });
                }
                catch
                {
                    // If Redis setup fails completely, register in-memory cache
                    RegisterInMemoryCache(services);
                }
            }
            else
            {
                // Use in-memory cache directly
                RegisterInMemoryCache(services);
            }

            return services;
        }

        private static bool ShouldAttemptRedisConnection(CacheConfiguration cacheConfig, IConfiguration configuration)
        {
            // Don't attempt Redis if:
            // 1. Connection string is null/empty
            // 2. Connection string is default localhost and we're in development
            // 3. Explicitly disabled via environment variable

            if (string.IsNullOrWhiteSpace(cacheConfig?.ConnectionString))
                return false;

            if (Environment.GetEnvironmentVariable("DISABLE_REDIS") == "true")
                return false;

            // Check if it's a real Redis configuration or just localhost default
            var connectionString = cacheConfig.ConnectionString.ToLower();
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            if (isDevelopment && connectionString.Contains("localhost:6379"))
            {
                // In development, only use Redis if explicitly configured differently
                return connectionString != "localhost:6379" && connectionString != "localhost:6379,abortconnect=false";
            }

            return true;
        }

        private static void RegisterInMemoryCache(IServiceCollection services)
        {
            services.AddSingleton<ICacheService>(sp =>
            {
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                var logger = sp.GetRequiredService<ILogger<InMemoryCacheService>>();
                var programLogger = sp.GetRequiredService<ILogger<Program>>();

                programLogger.LogInformation("🧠 Using in-memory cache service");
                return new InMemoryCacheService(memoryCache, logger);
            });
        }
    }
}