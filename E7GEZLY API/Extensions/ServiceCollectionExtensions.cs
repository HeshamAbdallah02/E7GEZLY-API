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
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IVerificationService, VerificationService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IVenueProfileService, VenueProfileService>();

            // Add memory cache
            services.AddMemoryCache();

            // Register HTTP client for Nominatim
            services.AddHttpClient<NominatimGeocodingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register geocoding services with decoration pattern
            services.AddScoped<NominatimGeocodingService>();

            // Note: The geocoding service will be decorated with distributed caching 
            // after Redis is configured in AddDistributedCaching
            services.AddScoped<IGeocodingService, NominatimGeocodingService>();

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

            // Try to add Redis, but fallback to in-memory if Redis fails
            try
            {
                var cacheConfig = configuration.GetSection("DistributedCache").Get<CacheConfiguration>()
                                 ?? new CacheConfiguration();

                if (!string.IsNullOrEmpty(cacheConfig.ConnectionString) && cacheConfig.ConnectionString != "localhost:6379")
                {
                    // Only try Redis if connection string is explicitly configured and not default
                    try
                    {
                        // Add Redis connection
                        services.AddSingleton<IConnectionMultiplexer>(sp =>
                        {
                            var configOptions = ConfigurationOptions.Parse(cacheConfig.ConnectionString);
                            configOptions.AbortOnConnectFail = false;
                            configOptions.ConnectRetry = 3;
                            configOptions.ConnectTimeout = 5000;

                            return ConnectionMultiplexer.Connect(configOptions);
                        });

                        // Add Redis cache service
                        services.AddSingleton<ICacheService, RedisCacheService>();

                        // Log that Redis is being used
                        var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
                        logger?.LogInformation("Using Redis cache service");
                    }
                    catch
                    {
                        // If Redis setup fails, fallback to in-memory
                        services.AddSingleton<ICacheService, InMemoryCacheService>();

                        var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
                        logger?.LogWarning("Redis setup failed, falling back to in-memory cache");
                    }
                }
                else
                {
                    // Use in-memory cache by default
                    services.AddSingleton<ICacheService, InMemoryCacheService>();

                    var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
                    logger?.LogInformation("Using in-memory cache service (development mode)");
                }
            }
            catch (Exception)
            {
                // If anything fails, use in-memory cache
                services.AddSingleton<ICacheService, InMemoryCacheService>();
            }

            return services;
        }
    }
}