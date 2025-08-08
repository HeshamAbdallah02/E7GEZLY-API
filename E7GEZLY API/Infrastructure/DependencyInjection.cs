using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Data;
using E7GEZLY_API.Domain.Repositories;
using E7GEZLY_API.Domain.Services;
using E7GEZLY_API.Infrastructure.Persistence;
using E7GEZLY_API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace E7GEZLY_API.Infrastructure
{
    /// <summary>
    /// Dependency injection configuration for Infrastructure layer
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Add Infrastructure layer services to the container
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            // Database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Application context interface
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IVenueRepository, VenueRepository>();
            services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Domain services - moved here from Application layer to follow Clean Architecture
            // Domain services contain business logic and should be registered in Infrastructure
            services.AddScoped<IVenueProfileCompletionService, VenueProfileCompletionService>();
            services.AddScoped<IUserVerificationService, UserVerificationService>();

            // AutoMapper for Domain <-> Model mapping
            // Temporarily disabled while fixing mapping issues
            // services.AddAutoMapper(typeof(DomainToModelMappingProfile));

            return services;
        }
    }
}