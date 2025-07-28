using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Communication;
using E7GEZLY_API.Services.Location;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            // Add memory cache
            services.AddMemoryCache();

            // Register HTTP client for Nominatim
            services.AddHttpClient<NominatimGeocodingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register geocoding services with decoration pattern
            services.AddScoped<NominatimGeocodingService>();
            services.AddScoped<IGeocodingService>(provider =>
            {
                var nominatimService = provider.GetRequiredService<NominatimGeocodingService>();
                var cache = provider.GetRequiredService<IMemoryCache>();
                var logger = provider.GetRequiredService<ILogger<CachedGeocodingService>>();

                return new CachedGeocodingService(nominatimService, cache, logger);
            });

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

        public static IServiceCollection AddControllersWithOptions(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            services.AddEndpointsApiExplorer();
            services.AddHealthChecks();

            return services;
        }
        public static IServiceCollection AddCommunicationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var useMockEmail = configuration.GetValue<bool>("Email:UseMockService");
            var useMockSms = configuration.GetValue<bool>("Sms:UseMockService", true); // Default to true since SMS not implemented yet

            // Email Service
            if (useMockEmail)
            {
                services.AddSingleton<IEmailService, MockEmailService>();
            }
            else
            {
                services.AddSingleton<IEmailService, SendGridEmailService>();
            }

            // SMS Service
            if (environment.IsDevelopment() || useMockSms)
            {
                services.AddSingleton<ISmsService, MockSmsService>();
            }
            else
            {
                // TODO: Implement real SMS service
                services.AddSingleton<ISmsService, MockSmsService>(); // Using mock until real service is implemented
            }

            return services;
        }
    }
}